using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// This corresponds to a CharacterClass ScriptableObject data object, containing the core gameplay data for
    /// a given class. 
    /// </summary>
    public enum CharacterTypeEnum
    {
        //heroes
        TANK,
        ARCHER,
        MAGE,
        ROGUE,

        //monsters
        IMP,
        IMP_BOSS,
    }
}
