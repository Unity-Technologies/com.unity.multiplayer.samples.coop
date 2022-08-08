using System;
using System.Collections.Generic;
using UnityEngine;
using Action = Unity.Multiplayer.Samples.BossRoom.Actions.Action;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class GameDataSource : MonoBehaviour
    {
        [Tooltip("All CharacterClass data should be slotted in here")]
        [SerializeField]
        private CharacterClass[] m_CharacterData;

        [SerializeField]
        Action m_GeneralChase;

        [Tooltip("All Action prototype scriptable objects should be slotted in here")]
        [SerializeField]
        private Action[] m_ActionPrototypes;

        private Dictionary<CharacterTypeEnum, CharacterClass> m_CharacterDataMap;

        /// <summary>
        /// static accessor for all GameData.
        /// </summary>
        public static GameDataSource Instance { get; private set; }

        public Action GetActionPrototypeByID(ActionID index)
        {
            return m_ActionPrototypes[index.ID];
        }

        /// <summary>
        /// Contents of the CharacterData list, indexed by CharacterType for convenience.
        /// </summary>
        public Dictionary<CharacterTypeEnum, CharacterClass> CharacterDataByType
        {
            get
            {
                if (m_CharacterDataMap == null)
                {
                    m_CharacterDataMap = new Dictionary<CharacterTypeEnum, CharacterClass>();
                    foreach (CharacterClass data in m_CharacterData)
                    {
                        if (m_CharacterDataMap.ContainsKey(data.CharacterType))
                        {
                            throw new System.Exception($"Duplicate character definition detected: {data.CharacterType}");
                        }
                        m_CharacterDataMap[data.CharacterType] = data;
                    }
                }
                return m_CharacterDataMap;
            }
        }

        public ActionID GeneralChaseActionID { get; set; }

        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined!");
            }



            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public ActionID GetIndexOfActionPrototype(Action action)
        {
            return new ActionID()
            {
                ID = Array.IndexOf(m_ActionPrototypes, action)
            };
        }
    }
}
