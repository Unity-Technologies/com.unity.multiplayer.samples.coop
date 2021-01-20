using UnityEngine;


namespace BossRoom
{
    /// <summary>
    /// Data representation of a Character, containing such things as its starting HP and Mana, and what attacks it can do. 
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/CharacterClass", order = 1)]
    public class CharacterClass : ScriptableObject
    {
        [Tooltip("which character this data represents")]
        public CharacterTypeEnum CharacterType;

        [Tooltip("skill1 is usually the character's default attack")]
        public ActionType Skill1;

        [Tooltip("skill2 is usually the character's secondary attack")]
        public ActionType Skill2;

        [Tooltip("skill3 is usually the character's unique or special attack")]
        public ActionType Skill3;

        [Tooltip("Starting HP of this character class")]
        public int BaseHP;

        [Tooltip("Starting Mana of this character class")]
        public int BaseMana;

        [Tooltip("Set to true if this represents an NPC, as opposed to a player.")]
        public bool IsNPC;

        [Tooltip("For NPCs, this will be used as the aggro radius at which enemies wake up and attack the player")]
        public float DetectRange;
    }
}


