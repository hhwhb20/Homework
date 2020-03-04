using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TacticGameX.Core._Role;
using System;
using TacticGameX.Core._Tile6;
using TacticGameX.GridEventSystem;
using System.Threading.Tasks;
using Dean.AI;
using TacticGameX.Serialization;
using static TacticGameX.Core._Role.Role;
using static TacticGameX.Core._RoleAction.RoleAction;
using static TacticGameX.InputControlSystem.Player_Control_Manager;
using TacticGameX.Animation;
using TacticGameX.Core._GridItem;

namespace TacticGameX.Core._Skill
{
    /// <summary>
    /// 角色动作
    /// </summary>
    public class Skill : IGridEventListener, ISkill
    {
        public class SkillAnimation
        {
            public List<string> Atkdata { get; set; } //攻击数据
            public List<string> Hitdata { get; set; } //受击数据
            public List<string> HitBackdata { get; set; } //受击数据
        }

        public static string tag_atkhit = "AttackHit"; //攻击动画的hit帧事件的
        public static string tag_hithit = "Hit"; //受击动画的hit帧事件

        /// <summary>
        /// 所属角色
        /// </summary>
        private IHasSkill me;
        public long world_id;
        private TableSkill tableskill;
        private bool _canUse;

        private bool _canSlience = true;

        public SkillAnimation skillViewData;

        public virtual int Priority { get; set; }
        public virtual bool CanSlience { get => _canSlience; }
        public IHasSkill Me { get => me; }
        public TableSkill tableSkill { get => tableskill; }
        public Dictionary<string, GridEvent> dictEvent = new Dictionary<string, GridEvent>();
        public SkillAnimation SkillViewData => skillViewData;
        //public ISkillTriggerSource skillTriggerSource { get; set; }

        //public Role triggleSource;
        //public TableSkill triggleSkill; //触发新技能的源技能


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="me"></param>
        /// <param name="world_id"></param>
        public virtual async Task Init(IHasSkill me, long world_id, TableSkill table)
        {
            this.world_id = world_id;
            this.me = me;
            this.tableskill = table;
            Priority = table.Priority;
            skillViewData = new SkillAnimation();
            if (table.DamageEffect == null)
            {
                if (me is IRole r)
                {
                    var modelTable = Table.GetTable<TableModel>(r.ModelId);
                    skillViewData.Atkdata = modelTable.Attack;
                    skillViewData.Hitdata = modelTable.Hit;
                    skillViewData.HitBackdata = modelTable.HitBack;
                }
                else if (table.ID != SkillManager.DoNothingid)
                {
                    Debug.LogError($"需要加功能");
                }
            }
            else
            {
                skillViewData.Atkdata = table.DamageEffect;
                skillViewData.Hitdata = table.HitEffect;
                skillViewData.HitBackdata = table.HitEffect;
            }

            System.Reflection.PropertyInfo[] infos = this.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var dieldInfos = this.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var info in infos)
            {
                if (table.ooINT.ContainsKey(info.Name)|| table.ooListInt.ContainsKey(info.Name))
                {
                    //给字段赋值
                    if (typeof(int) == info.PropertyType)
                    {
                        object value = table.ooINT[info.Name];
                        info.SetValue(this, Table.IntValue(value.ToString()));

                    }
                    else if (typeof(List<int>) == info.PropertyType)
                    {
                        object value = table.ooListInt[info.Name];
                        info.SetValue(this, value as List<int>);
                    }
                    else
                    {
                        Debug.Log($"参数:{info.Name}类型不匹配，赋值失败");
                    }
                }

            }

            foreach (var info in dieldInfos)
            {
                if (table.ooINT.ContainsKey(info.Name) || table.ooListInt.ContainsKey(info.Name))
                {
                    //给字段赋值
                    if (typeof(int) == info.FieldType)
                    {
                        object value = table.ooINT[info.Name];
                        info.SetValue(this, Table.IntValue(value.ToString()));

                    }
                    else if (typeof(List<int>) == info.FieldType)
                    {
                        object value = table.ooListInt[info.Name];
                        info.SetValue(this, value as List<int>);
                    }
                    else
                    {
                        Debug.Log($"参数:{info.Name}类型不匹配，赋值失败");
                    }
                }

            }
        }

        public virtual async Task SkillTrigger(string name)
        {
            Debug.Log($"[帧事件@@@@@]{name},发动者类型：{this.GetType()}, 发动者名字{this.Name}");
            if (dictEvent.ContainsKey(name))
            {
                var GM = GameWorld.GetWorld(world_id).gridEventManager;
                if (name == tag_atkhit)
                {
                    await dealAtkHit();
                }
                await GM.ExecuteEvent(dictEvent[name]);
                dictEvent.Remove(name);
            }
        }
        //处理攻击的hit点的表现处理
        public async Task dealAtkHit()
        {
            if (dictEvent[tag_atkhit] is GridEvent_Multiple_Damage dmgs)
            {
                foreach(var dmg in dmgs.dmgs)
                {
                    if (dmg.Victim is IHasAnimationManager has)
                    {
                        Animation_Damage hit = new Animation_Damage(dmg.Victim, dmg.damage.value, dmg.source, world_id, "Damage", null);
                        has.animationManager.Play(hit);
                    }
                }
            }
        }

        public virtual async Task CheckEvent()
        {
            if (dictEvent.Count > 0)
            {
                foreach(var name in dictEvent.Keys)
                {
                    Debug.Log($"{this.Name}动画等待{name}帧事件");
                }
            }
            await Extend_Methode.Extend.WaitUntil(() => (dictEvent == null || dictEvent.Count <= 0), 10, $"等待动画事件完成{dictEvent}");
        }

        public virtual async Task Skill_View(GridEvent e)
        {

        }

        /// <summary>
        /// 如果是，那么角色会先移动到点击的格子再行动，否则会直接进行行动
        /// </summary>
        public virtual bool Move_Before_Action => true;

        /// <summary>
        /// 行动类型
        /// </summary>
        public enum Action_Type
        {
            /// <summary>
            /// 无动作
            /// </summary>
            Move,
            /// <summary>
            /// 攻击类行为
            /// </summary>
            Attack,
            /// <summary>
            /// 被动类
            /// </summary>
            Passive,
            /// <summary>
            /// 替补类
            /// </summary>
            Bench,
            /// <summary>
            /// 事件类
            /// </summary>
            Event,
            /// <summary>
            /// 标签类
            /// </summary>
            Label,
            /// <summary>
            /// 没有行动
            /// </summary>
            NoAction,
            /// <summary>
            /// 没有行动
            /// </summary>
            QTE,
        }

        /// <summary>
        /// 本技能的行动分类，用来决定消耗哪种类型的点数,暂时作用不大，唯一作用是监听是否是攻击技能
        /// </summary>
        public virtual Action_Type Type { get; }


        private bool canUse = true;
        /// <summary>
        /// 是否可以使用
        /// </summary>
        public virtual bool CanUse { get => canUse; set => canUse = value; } //{ get => tableSkill.NeedEnergy<=Me.Current_Mana;}
        public virtual string Description { get; }
        public virtual string Name => tableskill?.Name;

        /// <summary>
        /// 如果地格上面没东西，或者是自己，还在移动范围内，则返回True
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool isCanDoSomeing(Tile tile, List<ITile> movable_area)
        {
            return (movable_area.Contains(tile));
        }
        /// <summary>
        /// 
        /// </summary>
        public abstract class GridEvent_RoleAction_Undefine
        {
            //？这里需要改成使用To_GridEvent_RoleAction去转化，外部由无权GridEvent_RoleAction_Undefine得到GridEvent_RoleAction
            //？必须提供一个目标，不过目标可以=null
            //public virtual GridEvent_RoleAction GridEvent_RoleAction { get; }

            /// <summary>
            /// 行动所有的目标所在地格
            /// </summary>
            public List<ITile> victimsTile;
            /// <summary>
            /// 根据一个具体的格子，转化成一个完整的Action
            /// </summary>
            /// <param name="tile"></param>
            /// <returns></returns>
            public abstract GridEvent_RoleAction To_GridEvent_RoleAction(ITile tile);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class GridEvent_RoleAction_Undefine<T> : GridEvent_RoleAction_Undefine where T : GridEvent_RoleAction
        {

            /// <summary>
            /// 行动
            /// </summary>
            public T gridEvent_RoleAction;
            //public override GridEvent_RoleAction GridEvent_RoleAction => gridEvent_RoleAction;
        }


        /// <summary>
        /// 如果人物移动到这个地格的话，将会执行的事件，如果本技能在该格子无法执行任何事件就会返回null
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual Task<Dictionary<ITile, GridEvent_RoleAction_Undefine>> GetEventIfAvailable(List<ITile> tiles, List<ITile> movable_area)
        {
            return null;
        }

        /// <summary>
        /// 事件触发机制下对目标释放一个技能的事件构造
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual Task<GridEvent_RoleAction> GetEventIfTrigger(List<ISkillTarget> targets, ISkillTriggerSource source, TableSkill triggerSkill = null)
        {
            //throw new NotImplementedException();
            return null;
        }

        /// <summary>
        /// 获取目标
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual Task<List<ISkillTarget>> GetSkillTargetsByTile(ITile tile)
        {
            //throw new NotImplementedException();
            return null;
        }



        /// <summary>
        /// 在此地格，此技能将可能会影响的范围,maxRange要返回最大距离
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual Dictionary<Hex, ITile> GetAtkRange(ITile tile, out int maxRange, bool onlyMax = false)
        {
            Dictionary<Hex, ITile> dict = new Dictionary<Hex, ITile>();
            maxRange = 0;
            if (Me is IRole r)
            {
                var max = tile.GetRange(r.Distance, false, onlyMax);
                var min = tile.GetRange(0);
                maxRange = r.Distance;
                foreach (var t in max)
                {
                    if (!min.Contains(t))
                    {
                        dict.Add(t.Coord, t);
                    }
                }
            }

            return dict;
        }

        #region AI用
        /// <summary>
        /// 威胁评估
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Dictionary<ITile, float>> AnalyseThreat()
        {
            return new Dictionary<ITile, float>();
        }

        /// <summary>
        /// 收益评估
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Dictionary<ITile, float>> AnalyseGain()
        {
            return new Dictionary<ITile, float>();
        }

        /// <summary>
        /// 受击评估
        /// </summary>
        /// <returns></returns>
        public virtual AIPlayer.Gain BeHurtGain(IDamageSource source, int value)
        {
            return new AIPlayer.Gain();
        }

        #endregion


        #region 监听者接口
        public virtual Task BeforeEvent(GridEvent e)
        {
            return Task.CompletedTask;
        }

        public virtual Task AfterEvent(GridEvent e)
        {
            return Task.CompletedTask;
        }

        public virtual Task Dispose()
        {
            throw new NotImplementedException();
        }



        #endregion
    }




}