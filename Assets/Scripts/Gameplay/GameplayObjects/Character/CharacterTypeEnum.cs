using System;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// This corresponds to a CharacterClass ScriptableObject data object, containing the core gameplay data for
    /// a given class.
    /// </summary>
    public enum CharacterTypeEnum
    {
        //heroes
        Tank,
        Archer,
        Mage,
        Rogue,

        //monsters
        Imp,
        ImpBoss,
        VandalImp
    }
}
