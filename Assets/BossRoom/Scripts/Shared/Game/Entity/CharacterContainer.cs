using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// The source of truth for a PC/NPCs CharacterClass. It is either set through the inspector, or it is networked
    /// and fetched from the CharacterRegistry.
    /// </summary>
    public class CharacterContainer : NetworkBehaviour
    {
        [Tooltip("Serialized for NPCs, or networked for PCs through NetworkCharacterGuidState.")]
        [SerializeField]
        CharacterClass m_CharacterClass;

        public CharacterClass CharacterClass => m_CharacterClass;

        [SerializeField]
        NetworkCharacterGuidState m_NetworkCharacterGuidState;

        [SerializeField]
        CharacterRegistry m_CharacterRegistry;

        Character m_Character;

        public GameObject CharacterGraphics => m_Character.Graphics;

        void Awake()
        {
            if (m_NetworkCharacterGuidState)
            {
                m_NetworkCharacterGuidState.CharacterGuidChanged += RegisterCharacter;
            }
        }

        void RegisterCharacter(Guid characterGuid)
        {
            // based on the Guid received, Character is fetched from CharacterRegistry
            if (!m_CharacterRegistry.TryGetCharacter(characterGuid, out m_Character))
            {
                Debug.LogError("Character not found!");
            }

            m_CharacterClass = m_Character.CharacterClass;
        }
    }
}
