using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacticGameX.Core._GridItem;
using TacticGameX.Core._Role;
using TacticGameX.Core._Tile6;
using TacticGameX.GridEventSystem;
using TacticGameX.Serialization;
using UnityEngine;
using static TacticGameX.Core._Skill.Skill;
using Random = UnityEngine.Random;

namespace TacticGameX.Core._Skill
{
    public class SkillManager  : IGridEventListener
    {
        public enum ActionType
        {
            AT_Initiative = 1, //主动
            AT_Passive,    //被动
            AT_Trigger,    //触发
        }
        //计算的参数类
        public class CalcMod
        {
            /// <summary>
            /// 整体的一个系数修正
            /// </summary>
            public float AllMod { get; set; }
            /// <summary>
            /// 攻击力的参数修正
            /// </summary>
            public float AtkMod { get; set; }
            /// <summary>
            /// 防御的参数修正
            /// </summary>
            public float DefMod { get; set; }
            /// <summary>
            /// 减甲值
            /// </summary>
            public int IngoreDef { get; set; }
            /// <summary>
            /// 是否助战
            /// </summary>
            public bool IsHelp { get; set; }
            /// <summary>
            /// 连击数量
            /// </summary>
            public int ComboNum { get; set; }
            /// <summary>
            /// 额外伤害
            /// </summary>
            public int ExtraDmg { get; set; }


            public CalcMod(TableSkill table)
            {
                AllMod = 1;
                AtkMod = 1;
                DefMod = 1;
                IsHelp = false;
                ComboNum = 0;
                ExtraDmg = 0;
            }

            public CalcMod()
            {
                AllMod = 1;
                AtkMod = 1;
                DefMod = 1;
                IsHelp = false;
                ComboNum = 0;
                ExtraDmg = 0;
            }

            public void Clear()
            {
                AllMod = 1;
                AtkMod = 1;
                DefMod = 1;
                IsHelp = false;
                ComboNum = 0;
                ExtraDmg = 0;
            }
        }
        #region 字段

        public static int DoNothingid = 0;
        public static int NormalAttackid = 1;
        public static int QTESkillid = 2;
        public static int MonsterNormalAttackid = 3; //先蓄力，下回合攻击
        public static int MonsterNormalAttack2id = 4; //直接攻击
        public static int FireCellSkillid = 130051;

        //private List<RoleAction> AllAction = new List<RoleAction>();
        private Dictionary<int, ISkill> AllAction = new Dictionary<int, ISkill>();

        private List<ISkill> actionByPriority = new List<ISkill>();

        /// <summary>
        /// 所属人物
        /// </summary>
        public IHasSkill Me;

        protected long world_id;
        private List<int> skills;
        protected int firstSkill;

        public int FirstSkill { get => firstSkill; }

        //Task<Dictionary<SkillManager.Action_State, SkillManager.Actionable_Area>> Get_Actionable_Area();
        //Task<(Dictionary<Hex, Tile> movedict, Dictionary<Hex, Tile> atkdict)> GetDangerousArea();

        private int last_Act_Time = 0;
        public int Last_Act_Time {
            get =>last_Act_Time;
            set => last_Act_Time = value; }
        /// <summary>
        /// 是否为已行动状态
        /// </summary>
        public bool Acted
        {
            get { return Last_Act_Time == 0; }
        }
        /// <summary>
        /// 是否为已行动状态
        /// </summary>
        public bool CanAct
        {
            get
            {
                return !Acted;
            }
        }
        public List<int> Skill { get; }

        //private Action_State action_State;

        /// <summary>
        /// 当前的状态
        /// </summary>
        public enum Action_State
        {
            /// <summary>
            /// 正在释放普通技能
            /// </summary>
            Normal,
            /// <summary>
            /// 正在释放大招
            /// </summary>
            Ultimate,
        }
        #endregion
        /// <summary>769
        /// 描述走到某个位置将会进行的行动的字典集合
        /// </summary>
        public class Actionable_Area
        {
            /// <summary>
            /// 可以进行行动的格子
            /// </summary>
            public Dictionary<ITile, GridEvent_RoleAction_Undefine> dict = new Dictionary<ITile, GridEvent_RoleAction_Undefine>();
            /// <summary>
            /// 无法进行行动但是仍然要高亮的格子
            /// </summary>
            public List<ITile> unable_to_act_dict = new List<ITile>();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="me"></param>
        public virtual async Task Init(IHasSkill me, long world_id, Dictionary<int, int> skillLevel)
        {
            Me = me;
            this.world_id = world_id;
            firstSkill = -1;
            skills = new List<int>();
            var GM = GameWorld.GetWorld(this.world_id).gridEventManager;
            GM.AddListener<GridEvent_Multiple_Death>(this);
            GM.AddListener<GridEvent_Game_Over>(this);
            bool haveAtk = false;
            await ReSetActState();

            //--------添加技能-----------------------------------
                foreach (KeyValuePair<int, int> pair in skillLevel)
            {
                TableSkill table = TableManager.GetInstance().GetTable<TableSkill>(pair.Key.ToString());
                if (table == null)
                {
                    if (pair.Key != 0)
                    {
                        Debug.LogError($"[Error]技能编号{pair.Key}不存在失败");
                    }
                    continue;
                }
                try
                {
                    Skill action = Extend_Methode.Extend.Create_Object_By_Class_Name<Skill>(typeof(Skill).Namespace + "." + table.ClassName);
                    //action.tableSkill = table;
                    //action.Priority = table.Priority;
                    await action.Init(me, world_id, table);
                    if (action.Type == Action_Type.Attack)
                    {
                        haveAtk = true;
                    }
                    AllAction.Add(pair.Key, action);
                    actionByPriority.Add(action);
                    skills.Add(pair.Key);
                }
                catch (Exception e)
                {
                    if (pair.Key != 0)
                    {
                        Debug.LogError($"[Error]技能编号{pair.Key}创建失败，可能是Skill_{table.ClassName}不存在\n{e}");
                    }
                    Debug.LogError($"[Error]技能编号{pair.Key}创建失败，可能是Skill_{table.ClassName}不存在\n{e}");
                    continue;
                }
            }
            //skills = new List<int>(skillLevel.Keys);
            /******************************添加休息和啥也不干****************************/ //休息技能砍掉，因为显性设计有冲突
                                                                                  //for (int i = 1; i <= 1; i++)
                                                                                  //{
                                                                                  //if (AllAction.ContainsKey(i)) continue;
                                                                                  //TableSkill table = TableManager.GetInstance().GetTable<TableSkill>(i.ToString());
            TableSkill table2 = new TableSkill();
            table2.ID = DoNothingid;
            //table2.ClassName = "NewDoNothing";
            table2.TileIcon = "Move";
            try
            {
                Skill action = Extend_Methode.Extend.Create_Object_By_Class_Name<Skill>(typeof(Skill).Namespace + "." + "NewDoNothing");
                //action.tableSkill = table;
                //action.Priority = table.Priority;
                await action.Init(me, world_id, table2);
                AllAction.Add(table2.ID, action);
                actionByPriority.Add(action);
                skills.Add(table2.ID);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Error]技能编号{0}创建失败，可能是NewDoNothing不存在\n{e}");
                //continue;
            }
            if (me is IRole r && r.Type == Role.RoleType.RT_hero && !haveAtk)
            {
                //skillLevel.Add(130038, 1);
                //普攻
                TableSkill table3 = new TableSkill();
                table3.ID = NormalAttackid;
                //table2.ClassName = "NewDoNothing";
                table3.TileIcon = "Atk";
                try
                {
                    Skill action = Extend_Methode.Extend.Create_Object_By_Class_Name<Skill>(typeof(Skill).Namespace + "." + "NormalAttack");
                    //action.tableSkill = table;
                    //action.Priority = table.Priority;
                    await action.Init(me, world_id, table3);
                    AllAction.Add(table3.ID, action);
                    actionByPriority.Add(action);
                    skills.Add(table3.ID);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Error]技能编号{0}创建失败，可能是NormalAttack不存在\n{e}");
                    //continue;
                }
                //QTE
                TableSkill table4 = new TableSkill();
                table4.ID = QTESkillid;
                //table2.ClassName = "NewDoNothing";
                table4.TileIcon = "Atk";
                if (table4 == null)
                {
                    Debug.LogError($"[Error]技能编号0不存在失败");
                    //continue;
                }
                try
                {
                    Skill action = Extend_Methode.Extend.Create_Object_By_Class_Name<Skill>(typeof(Skill).Namespace + "." + "QTESkill");
                    //action.tableSkill = table;
                    //action.Priority = table.Priority;
                    await action.Init(me, world_id, table4);
                    AllAction.Add(table4.ID, action);
                    actionByPriority.Add(action);
                    skills.Add(table4.ID);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Error]技能编号{0}创建失败，可能是NormalAttack不存在\n{e}");
                    //continue;
                }
                //}
            }
            else if(me is IRole r2 && r2.Type == Role.RoleType.RT_monster && !haveAtk)
            {
                TableMonster tableMonster = Table.GetTable<TableMonster>(r2.MyTableID);
                //普攻
                TableSkill table3 = new TableSkill();
                table3.ID = MonsterNormalAttack2id;
                //table2.ClassName = "NewDoNothing";
                table3.TileIcon = "Atk";
                try
                {
                    Skill action = Extend_Methode.Extend.Create_Object_By_Class_Name<Skill>(typeof(Skill).Namespace + "." + "MonsterNormalAttack2");
                    //action.tableSkill = table;
                    //action.Priority = table.Priority;
                    await action.Init(me, world_id, table3);
                    AllAction.Add(table3.ID, action);
                    actionByPriority.Add(action);
                    skills.Add(table3.ID);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Error]技能编号{MonsterNormalAttack2id}创建失败，可能是MonsterNormalAttack2不存在\n{e}");
                    //continue;
                }
                if (tableMonster.mode == 1) //蓄力在攻击
                {
                    //普攻
                    TableSkill table4 = new TableSkill();
                    table4.ID = MonsterNormalAttackid;
                    //table2.ClassName = "NewDoNothing";
                    table4.TileIcon = "Atk";
                    try
                    {
                        Skill action = Extend_Methode.Extend.Create_Object_By_Class_Name<Skill>(typeof(Skill).Namespace + "." + "MonsterNormalAttack");
                        //action.tableSkill = table;
                        //action.Priority = table.Priority;
                        await action.Init(me, world_id, table4);
                        AllAction.Add(table4.ID, action);
                        actionByPriority.Add(action);
                        skills.Add(table4.ID);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Error]技能编号{MonsterNormalAttackid}创建失败，可能是MonsterNormalAttack不存在\n{e}");
                        //continue;
                    }
                }
            }

            actionByPriority.Sort(CMPPriority);
        }

        public async Task ReSetActState()
        {
            if (Me is IRole r && r.Type == Role.RoleType.RT_monster)
            {
                if (r.RoundNumber > 0)
                {
                    var turn = GameWorld.GetWorld(world_id).GameManager.Turn + 1;
                    if (r.IntervalRound >= 0 && turn > 0 && turn % (r.IntervalRound+1) == 0)
                    {
                        last_Act_Time = r.RoundNumber;
                        var index = turn / (r.IntervalRound + 1) - 1;
                        if (r.AddSkill.Count > index && r.AddSkill[index] > 0)
                        {
                            firstSkill = r.AddSkill[index];
                        }
                        else
                        {
                            firstSkill = -1;
                        }
                    }
                    else
                    {
                        last_Act_Time = 1;
                    }
                }
                else
                {
                    last_Act_Time = 1;
                }
            }
            else
            {
                last_Act_Time = 1;
            }
        }

        /// <summary>
        /// 对目标动态释放一个技能,这个技能人身上必须有
        /// </summary>
        /// <returns></returns>
        public async Task UseSkill(int skillid, List<ISkillTarget> targets, ISkillTriggerSource source, TableSkill table)
        {
            if (!AllAction.ContainsKey(skillid)) return;
            if (table != null && table.ID == skillid)
            {
                Debug.LogError($"技能{table.ID}要触发{skillid},自己触发自己是什么操作？");
                return;
            }
            var GM = GameWorld.GetWorld(world_id).gridEventManager;
            var action = AllAction[skillid];
            //action.triggleSource = source;
            //action.triggleSkill = table;
            var actevent = await action.GetEventIfTrigger(targets, source, table);
            if (actevent == null)
            {
                Debug.LogError($"技能{skillid}没实现GetEventIfTrigger，却要触发该技能，一定是哪里出了问题");
            }
            else
            {
                actevent.GM = GM;
                await GM.ExecuteEvent(actevent);
            }
        }

        /// <summary>
        /// 优先级越高，越靠前。 技能id越小，越靠前
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int CMPPriority(ISkill a, ISkill b)
        {
            if (a.Priority != b.Priority)
            {
                return a.Priority > b.Priority ? -1 : 1;
            }
            if (a.tableSkill == null)
            {
                return 1;
            }
            else if (b.tableSkill == null)
            {
                return -1;
            }
            else
            {
                return a.tableSkill.ID < b.tableSkill.ID ? -1 : 1;
            }
        }

        #region 快捷判断和搜索
        /// <summary>
        /// 是否有可以进行的行动
        /// </summary>
        //public bool CanAct => AllAction.Exists(x => x.CanUse);
        public bool CanSkillAct()
        {
            foreach (var act in AllAction.Values)
            {
                if (act.CanUse)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 所有可以进行的行动
        /// </summary>
        public List<ISkill> Available_Action()
        {
            List<ISkill> list = new List<ISkill>();
            foreach (var act in AllAction.Values)
            {
                if (act.CanUse)
                {
                    list.Add(act);
                }
            }
            return list;
        }

        /// <summary>
        /// 在一个格子上是否可以对特定的敌人使用技能
        /// </summary>
        /// <param name="tile"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public bool CanUseSkillToOne(ITile tile,ISkillTarget skillTarget, Action_Type action_Type)
        {
            if (skillTarget == null|| tile == null)
                return false;
            foreach (var skill in Available_Action())
            {
                if (skill.Type == action_Type)
                {
                    if (skillTarget is ITileContainable containable && containable.CurrentTile!=null
                        && skill.Me is IRole role && tile.GetDistance(containable.CurrentTile) <= role.Distance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }




        /// <summary>
        /// 所有行动
        /// </summary>
        public List<ISkill> All_Action_Sort_By_Priority => actionByPriority;

        public int Priority => 100;

        public ISkill GetRoleActionByID(int id)
        {
            if (AllAction.ContainsKey(id))
            {
                return AllAction[id];
            }
            return null;
        }

        public virtual async Task Dispose()
        {
            foreach(var skill in actionByPriority)
            {
                await skill.Dispose();
            }
        }

        /// <summary>
        /// 清理当前的技能（变身）
        /// </summary>
        internal async Task ClearRoleAction()
        {
            await Dispose();
            AllAction.Clear();
            All_Action_Sort_By_Priority.Clear();
        }
        #endregion
        #region  技能计算公式
        /// <summary>
        /// 是否命中
        /// </summary>
        /// <returns></returns>
        public static bool CheckHit(IHasSkill atker, ISkillTarget tar)
        {
            if (atker is IRole r1 && tar is IRole r2) //命中-闪避
            {
                int rate = Math.Max(0, r1.HitRate - r2.DodgeRate);
                return UnityEngine.Random.Range(0, 10000) < rate;
            }

            return true;
        }
        /// <summary>
        /// 是否暴击
        /// </summary>
        /// <returns></returns>
        public static bool CheckCrit(IHasSkill atker, ISkillTarget tar)
        {
            if (atker is IRole r1 && tar is IRole r2) //暴击率-免爆率
            {
                int rate = Math.Max(0, r1.Crit - r2.NoCrit);
                return UnityEngine.Random.Range(0, 10000) < rate;
            }
            else if (atker is IRole r3)
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// 伤害公式
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="source"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static int DamageCalc(IDamagable victim, IDamageSource source, CalcMod mod)
        {
            //伤害公式是（攻击力*攻击力系数-(防御力-忽略防御值)*防御力系数 + 助战伤害 + 连击伤害 + 额外伤害）* 总体系数,暂时先这样
            if (victim is IRole r1 && source is IRole r2)
            {
                float crit = CheckCrit(r1, r2) ? r1.CritHurt * 1.0f / 10000 : 1.0f;
                float damage = r2.Current_Attack * mod.AtkMod - Math.Max(0, r1.Defense - mod.IngoreDef) * mod.DefMod;
                damage = Math.Max(1, damage);
                float helpdmg = mod.IsHelp ? HelpCalc(r2, r1, damage) : 0;
                float combodmg = ComboCalc(r2, r1, mod.ComboNum);
                float total = (damage + helpdmg + combodmg + mod.ExtraDmg) * mod.AllMod * crit;
                total = Mathf.Max(1.0f, total);
                return (int)Math.Ceiling((double)total);
            }
            else if (source is IRole r3)
            {
                float crit = 1.0f;
                float damage = r3.Current_Attack * mod.AtkMod;
                float helpdmg = mod.IsHelp ? HelpCalc(source, victim, damage) : 0;
                float combodmg = ComboCalc(source, victim, mod.ComboNum);
                float total = (damage + helpdmg + combodmg + mod.ExtraDmg) * mod.AllMod * crit;
                total = Mathf.Max(1.0f, total);
                return (int)Math.Ceiling((double)total);
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 伤害公式预算
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="source"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static int PreDamageCalc(IDamagable victim, IDamageSource source, CalcMod mod)
        {
            //伤害公式是（攻击力*攻击力系数-(防御力-忽略防御值)*防御力系数 + 助战伤害 + 连击伤害 + 额外伤害）* 总体系数,暂时先这样
            if (victim is IRole r1 && source is IRole r2)
            {
                float crit =1.0f;
                float damage = r2.Current_Attack * mod.AtkMod - Math.Max(0, r1.Defense - mod.IngoreDef) * mod.DefMod;
                damage = Math.Max(1, damage);
                float helpdmg = mod.IsHelp ? HelpCalc(r2, r1, damage) : 0;
                float combodmg = ComboCalc(r2, r1, mod.ComboNum);
                float total = (damage + helpdmg + combodmg + mod.ExtraDmg) * mod.AllMod * crit;
                total = Mathf.Max(1.0f, total);
                return (int)Math.Ceiling((double)total);
            }
            else if(source is IRole r3)
            {
                float crit = 1.0f;
                float damage = r3.Current_Attack * mod.AtkMod;
                float helpdmg = mod.IsHelp ? HelpCalc(source, victim, damage) : 0;
                float combodmg = ComboCalc(source, victim, mod.ComboNum);
                float total = (damage + helpdmg + combodmg + mod.ExtraDmg) * mod.AllMod * crit;
                total = Mathf.Max(1.0f, total);
                return (int)Math.Ceiling((double)total);
            }
            else
            {
                return 0;
            }
        }

//         [UnityEditor.MenuItem("Test/Ceil")]
//         public static void Test()
//         {
//             Debug.Log($"[Test000000000000000]{Math.Ceiling(24.5)}");
//         }
        /// <summary>
        /// 击退公式
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="source"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static int HitBackCalc(IHasSkill atker, ISkillTarget tar)
        {
            if (atker is IRole r1 && tar is IRole r2)
            {
                return Math.Max(0, r1.HitBack - r2.BackStop);
            }
            else if (atker is IRole r3)
            {
                return r3.HitBack;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 助战公式
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="source"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static float HelpCalc(IDamageSource atker, IDamagable tar, float damage)
        {
            if (atker is IRole r1 && tar is IRole r2)
            {
                float value = 0;
                if (r1.HelpAddDamageType == 1 && r2.HelpSubDamageType == 1)
                {
                    value = r1.HelpAddDamage - r2.HelpSubDamage;
                }
                else if (r1.HelpAddDamageType == 2 && r2.HelpSubDamageType == 1)
                {
                    value = damage * r1.HelpAddDamage / 10000f - r2.HelpSubDamage;
                }
                else if (r1.HelpAddDamageType == 1 && r2.HelpSubDamageType == 2)
                {
                    value = r1.HelpAddDamage - damage * r2.HelpSubDamage / 10000f;
                }
                else if (r1.HelpAddDamageType == 2 && r2.HelpSubDamageType == 2)
                {
                    value = damage * (r1.HelpAddDamage - r2.HelpSubDamage) / 10000f;
                }

                return Mathf.Max(0, value);
            }
            else if (atker is IRole r3)
            {
                float value = 0;
                if (r3.HelpAddDamageType == 1)
                {
                    value = r3.HelpAddDamage;
                }
                else if (r3.HelpAddDamageType == 2 )
                {
                    value = damage * r3.HelpAddDamage / 10000f;
                }

                return Mathf.Max(0, value);
            }

            return 0;
        }

        /// <summary>
        /// 连击公式
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="source"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static float ComboCalc(IDamageSource atker, IDamagable tar, int combo)
        {
            if (combo <= 0) return 0;
            if (atker is IRole r1 && tar is IRole r2)
            {
                float value = 0;
                if (r1.HelpAddDamageType == 1 && r2.HelpSubDamageType == 1)
                {
                    value = combo + r1.HelpAddDamage - r2.HelpSubDamage;
                }
                else if (r1.HelpAddDamageType == 2 && r2.HelpSubDamageType == 1)
                {
                    value = combo * r1.HelpAddDamage / 10000f - r2.HelpSubDamage;
                }
                else if (r1.HelpAddDamageType == 1 && r2.HelpSubDamageType == 2)
                {
                    value = r1.HelpAddDamage - combo * r2.HelpSubDamage / 10000f;
                }
                else if (r1.HelpAddDamageType == 2 && r2.HelpSubDamageType == 2)
                {
                    value = combo * (r1.HelpAddDamage - r2.HelpSubDamage) / 10000f;
                }

                return Mathf.Max(0, value);
            }
            else if(atker is IRole r3)
            {
                float value = 0;
                if (r3.HelpAddDamageType == 1)
                {
                    value = combo + r3.HelpAddDamage;
                }
                else if (r3.HelpAddDamageType == 2)
                {
                    value = combo * r3.HelpAddDamage / 10000f;
                }

                return Mathf.Max(0, value);
            }

            return 0;
        }

        public virtual async Task BeforeEvent(GridEvent e)
        {
            //throw new NotImplementedException();
        }

        public virtual async Task AfterEvent(GridEvent e)
        {
            //throw new NotImplementedException();
            if (e is GridEvent_Multiple_Death deaths)
            {
                foreach(var d in deaths.events)
                {
                    if (d.victim is IHasSkill has && has == Me)
                    {
                        await this.Dispose();
                        break;
                    }
                }
            }
            else if (e is GridEvent_Game_Over)
            {
                await this.Dispose();
            }
        }
        #endregion
    }
}
