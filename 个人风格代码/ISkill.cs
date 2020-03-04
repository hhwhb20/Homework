using Dean.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacticGameX.Core._Tile6;
using TacticGameX.GridEventSystem;
using TacticGameX.Serialization;
using static TacticGameX.Core._Skill.Skill;
//using static TacticGameX.Core._Skill;

namespace TacticGameX.Core._Skill
{
    public interface ISkill
    {
        IHasSkill Me { get; }
        TableSkill tableSkill { get; }
        Action_Type Type { get; }
        int Priority { get; }
        bool Move_Before_Action { get; }
        SkillAnimation SkillViewData{ get; }
        Task Skill_View(GridEvent e);
        Task Init(IHasSkill role, long world_id, TableSkill table);
        bool CanUse { get; set; }
        bool CanSlience { get; }
        Task<GridEvent_RoleAction> GetEventIfTrigger(List<ISkillTarget> targets, ISkillTriggerSource source, TableSkill triggerSkill = null);
        Task<Dictionary<ITile, GridEvent_RoleAction_Undefine>> GetEventIfAvailable(List<ITile> tiles, List<ITile> movable_area);
        Dictionary<Hex, ITile> GetAtkRange(ITile tile, out int maxRange, bool onlyMax = false);
        Task<Dictionary<ITile, float>> AnalyseThreat();
        Task<Dictionary<ITile, float>> AnalyseGain();
        AIPlayer.Gain BeHurtGain(IDamageSource source, int value);
        Task Dispose();
        Task SkillTrigger(string name);
    }
}
