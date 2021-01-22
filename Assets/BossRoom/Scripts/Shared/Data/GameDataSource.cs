using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    public class GameDataSource : MonoBehaviour
    {
        [Tooltip("All CharacterClass data should be slotted in here")]
        [SerializeField]
        private CharacterClass[] CharacterData;

        [Tooltip("All ActionDescription data should be slotted in here")]
        [SerializeField]
        private ActionDescription[] ActionData;

        private Dictionary<CharacterTypeEnum, CharacterClass> m_CharacterDataMap;
        private Dictionary<ActionType, ActionDescription> m_ActionDataMap;

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
                    foreach (CharacterClass data in CharacterData)
                    {
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
                    foreach (ActionDescription data in ActionData)
                    {
                        m_ActionDataMap[data.ActionTypeEnum] = data;
                    }
                }
                return m_ActionDataMap;
            }
        }

        /// <summary>
        /// static accessor for all GameData. 
        /// </summary>
        public static GameDataSource s_Instance;

        private void Awake()
        {
            if (s_Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined!");
            }

            Object.DontDestroyOnLoad(gameObject);
            s_Instance = this;
        }

    }
}

