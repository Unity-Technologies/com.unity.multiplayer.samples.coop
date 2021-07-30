using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// The source of truth for a PC/NPCs CharacterClass.
    /// </summary>
    public class CharacterClassContainer : MonoBehaviour
    {
        [SerializeField]
        NetworkCharacterTypeState m_NetworkCharacterTypeState;

        [SerializeField]
        CharacterClass m_CharacterClass;

        public CharacterClass CharacterClass => m_CharacterClass;

        void Awake()
        {
            if (m_NetworkCharacterTypeState)
            {
                m_NetworkCharacterTypeState.CharacterType.OnValueChanged += OnValueChanged;
            }
        }

        void OnValueChanged(CharacterTypeEnum previousvalue, CharacterTypeEnum newvalue)
        {
            m_CharacterClass = GameDataSource.Instance.CharacterDataByType[newvalue];
        }

        /*public void SetCharacterClass(CharacterClass characterClass)
        {
            m_CharacterClass = characterClass;
        }*/
    }
}
