using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacticGameX.Core._Tile6;
using TacticGameX.GridEventSystem;

namespace TacticGameX.Core._Skill
{
    public interface ITileContainableHasSkill : IHasSkill, ITileContainable
    {
        TileContainableSkillManager skillManager { get; }
        //string Name { get; }
    }
}
