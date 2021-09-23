using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class GameDataSource : MonoBehaviour
    {
        [Tooltip("All CharacterClass data should be slotted in here")]
        [SerializeField]
        private CharacterClass[] m_CharacterData;

        [Tooltip("All ActionDescription data should be slotted in here")]
        [SerializeField]
        private ActionDescription[] m_ActionData;

        private Dictionary<CharacterTypeEnum, CharacterClass> m_CharacterDataMap;
        private Dictionary<ActionType, ActionDescription> m_ActionDataMap;

        /// <summary>
        /// static accessor for all GameData.
        /// </summary>
        public static GameDataSource Instance { get; private set; }

        /// <summary>
        /// Contents of the CharacterData list, indexed by CharacterType for convenience.
        /// </summary>
        public Dictionary<CharacterTypeEnum, CharacterClass> CharacterDataByType
        {
            get
            {
                if( m_CharacterDataMap == null )
                {
                    m_CharacterDataMap = new Dictionary<CharacterTypeEnum, CharacterClass>();
                    foreach (CharacterClass data in m_CharacterData)
                    {
                        if( m_CharacterDataMap.ContainsKey(data.CharacterType))
                        {
                            throw new System.Exception($"Duplicate character definition detected: {data.CharacterType}");
                        }
                        m_CharacterDataMap[data.CharacterType] = data;
                    }
                }
                return m_CharacterDataMap;
            }
        }

        /// <summary>
        /// Contents of the ActionData list, indexed by ActionType for convenience.
        /// </summary>
        public Dictionary<ActionType, ActionDescription> ActionDataByType
        {
            get
            {
                if( m_ActionDataMap == null )
                {
                    m_ActionDataMap = new Dictionary<ActionType, ActionDescription>();
                    foreach (ActionDescription data in m_ActionData)
                    {
                        if (m_ActionDataMap.ContainsKey(data.ActionTypeEnum))
                        {
                            throw new System.Exception($"Duplicate action definition detected: {data.ActionTypeEnum}");
                        }
                        m_ActionDataMap[data.ActionTypeEnum] = data;
                    }
                }
                return m_ActionDataMap;
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined!");
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
}
