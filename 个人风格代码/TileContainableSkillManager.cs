using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacticGameX.Core._Tile6;
using TacticGameX.GridEventSystem;
using UnityEngine;
using static TacticGameX.Core._Skill.Skill;

namespace TacticGameX.Core._Skill
{
    public class TileContainableSkillManager : SkillManager
    {
        //Task<Dictionary<SkillManager.Action_State, SkillManager.Actionable_Area>> Get_Actionable_Area();
        //Task<(Dictionary<Hex, Tile> movedict, Dictionary<Hex, Tile> atkdict)> GetDangerousArea();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="me"></param>
        public override async Task Init(IHasSkill me, long world_id, Dictionary<int, int> skillLevel)
        {
            await base.Init(me, world_id, skillLevel);
        }

        /// <summary>
        /// 获取可移动范围内的格子并判断将会进行的行动
        /// </summary>
        public async Task<Dictionary<SkillManager.Action_State, SkillManager.Actionable_Area>> Get_Actionable_Area()
        {
            GameWorld gameWorld = GameWorld.GetWorld(world_id);
            GridEventManager gm = gameWorld.gridEventManager;

            Dictionary<SkillManager.Action_State, SkillManager.Actionable_Area> r = new Dictionary<SkillManager.Action_State, SkillManager.Actionable_Area>();

            //获取所有可以移动到的位置
            List<Tile> all_tile = gameWorld.TileManager.AllTile;
            //对每个位置判断“如果移动到这个位置则会进行的行为”
            Dictionary<ITile, GridEvent_RoleAction_Undefine> dict = new Dictionary<ITile, GridEvent_RoleAction_Undefine>();
            //可移动范围
            List<ITile> movable_area = new List<ITile>();
            if (Me is IMovable move)
            {
                movable_area = await move.Get_Movalbe_Area();
            }
            //await Get_Movalbe_Area();

            //仍然需要问的地格
            List<ITile> to_ask = all_tile.ConvertAll<ITile>((t) => t as ITile);


            //按照优先度遍历行动
            foreach (var action in All_Action_Sort_By_Priority)
            {
                var type = action.Type; //(Skill.Action_Type)action.tableSkill.Type;
                if (type != Action_Type.Move && type != Action_Type.Attack) continue;
                if (!action.CanUse) continue;
                //要求每个行动返回一个字典
                foreach (KeyValuePair<ITile, GridEvent_RoleAction_Undefine> item in await action.GetEventIfAvailable(to_ask, movable_area))
                {
                    //如果已经有了，就不需要继续问下面的了
                    to_ask.Remove(item.Key);
                    //加入可选择的行动列表
                    if (!dict.ContainsKey(item.Key))
                    {
                        dict.Add(item.Key, item.Value);
                    }
                    else if(firstSkill > 0 && action.tableSkill.ID == firstSkill && Last_Act_Time == 1)
                    {
                        dict[item.Key] = item.Value;
                    }
                }
            }


            r.Add(SkillManager.Action_State.Normal, new Actionable_Area() { dict = dict, unable_to_act_dict = movable_area.FindAll(x => !dict.ContainsKey(x)) });

            return r;
        }

        /// <summary>
        /// 获取危险范围,因为怪是role来做的，所以先暂时这么写
        /// </summary>
        /// <param name="imaginal_center">如果为null，将会使用物体的当前位置</param>
        public async Task<(Dictionary<Hex, ITile> movedict, Dictionary<Hex, ITile> atkdict)> GetDangerousArea()
        {
            Dictionary<Hex, ITile> tileDict = new Dictionary<Hex, ITile>();
            List<ITile> movelist = new List<ITile>();
            if (Me is IMovable movable)
            {
                movelist = await movable.Get_Movalbe_Area();
            }
                //await this.Get_Movalbe_Area();
            Dictionary<Hex, ITile> moveDict = new Dictionary<Hex, ITile>();
            var allAction = this.All_Action_Sort_By_Priority;
            foreach (var m in movelist)
            {
                moveDict.Add(m.Coord, m);
                foreach (var action in allAction)
                {
                    int range = 0;
                    var dict = action.GetAtkRange(m, out range);
                    foreach (var t in dict.Values)
                    {
                        if (!tileDict.ContainsKey(t.Coord))
                        {
                            tileDict.Add(t.Coord, t);
                        }
                    }
                }
            }
            return (moveDict, tileDict);
        }
    }
}
