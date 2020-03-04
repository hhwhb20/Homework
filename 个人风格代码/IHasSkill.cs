using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacticGameX.Animation;
using TacticGameX.Core._Tile6;
using TacticGameX.GridEventSystem;
using UnityEngine;

namespace TacticGameX.Core._Skill
{
    public interface IHasSkill : IHasAnimationManager
    {
        SkillManager skillManager { get; }
        string Name { get; }
        Vector3 SkillerPos { get; }
//         Task<Dictionary<SkillManager.Action_State, SkillManager.Actionable_Area>> Get_Actionable_Area();
//         Task<(Dictionary<Hex, Tile> movedict, Dictionary<Hex, Tile> atkdict)> GetDangerousArea();
//         int Last_Act_Time {get;set;}
//         bool Acted { get; }
//         List<int> Skill { get; }
//         bool CanAct { get; }
        //void BeforeAction();
    }
}
