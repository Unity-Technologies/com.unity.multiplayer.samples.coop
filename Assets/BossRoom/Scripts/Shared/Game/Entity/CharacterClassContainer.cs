using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// The source of truth for a PC/NPCs CharacterClass.
    /// </summary>
    public class CharacterClassContainer : MonoBehaviour
    {
        [SerializeField]
        CharacterClass m_CharacterClass;

        public CharacterClass CharacterClass => m_CharacterClass;

        public void SetCharacterClass(CharacterClass characterClass)
        {
            m_CharacterClass = characterClass;
        }
    }
}
