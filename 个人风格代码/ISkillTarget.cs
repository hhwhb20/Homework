﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TacticGameX.Core._Skill
{
    public interface ISkillTarget
    {
        string Name { get; }
        int KUID { get; }
    }
}
