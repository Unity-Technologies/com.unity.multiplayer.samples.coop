using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;
using Action = Unity.BossRoom.Gameplay.Actions.Action;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class GameDataSource : MonoBehaviour
    {
        /// <summary>
        /// static accessor for all GameData.
        /// </summary>
        public static GameDataSource Instance { get; private set; }

        [Header("Character classes")]
        [Tooltip("All CharacterClass data should be slotted in here")]
        [SerializeField]
        private CharacterClass[] m_CharacterData;

        Dictionary<CharacterTypeEnum, CharacterClass> m_CharacterDataMap;

        //Actions that are directly listed here will get automatically assigned ActionIDs and they don't need to be a part of m_ActionPrototypes array
        [Header("Common action prototypes")]
        [SerializeField]
        Action m_GeneralChaseActionPrototype;

        [SerializeField]
        Action m_GeneralTargetActionPrototype;

        [SerializeField]
        Action m_Emote1ActionPrototype;

        [SerializeField]
        Action m_Emote2ActionPrototype;

        [SerializeField]
        Action m_Emote3ActionPrototype;

        [SerializeField]
        Action m_Emote4ActionPrototype;

        [SerializeField]
        Action m_ReviveActionPrototype;

        [SerializeField]
        Action m_StunnedActionPrototype;

        [SerializeField]
        Action m_DropActionPrototype;

        [SerializeField]
        Action m_PickUpActionPrototype;

        [Tooltip("All Action prototype scriptable objects should be slotted in here")]
        [SerializeField]
        private Action[] m_ActionPrototypes;

        public Action GeneralChaseActionPrototype => m_GeneralChaseActionPrototype;

        public Action GeneralTargetActionPrototype => m_GeneralTargetActionPrototype;

        public Action Emote1ActionPrototype => m_Emote1ActionPrototype;

        public Action Emote2ActionPrototype => m_Emote2ActionPrototype;

        public Action Emote3ActionPrototype => m_Emote3ActionPrototype;

        public Action Emote4ActionPrototype => m_Emote4ActionPrototype;

        public Action ReviveActionPrototype => m_ReviveActionPrototype;

        public Action StunnedActionPrototype => m_StunnedActionPrototype;

        public Action DropActionPrototype => m_DropActionPrototype;
        public Action PickUpActionPrototype => m_PickUpActionPrototype;

        List<Action> m_AllActions;

        public Action GetActionPrototypeByID(ActionID index)
        {
            return m_AllActions[index.ID];
        }

        public bool TryGetActionPrototypeByID(ActionID index, out Action action)
        {
            for (int i = 0; i < m_AllActions.Count; i++)
            {
                if (m_AllActions[i].ActionID == index)
                {
                    action = m_AllActions[i];
                    return true;
                }
            }

            action = null;
            return false;
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

        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined!");
            }

            BuildActionIDs();

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        void BuildActionIDs()
        {
            var uniqueActions = new HashSet<Action>(m_ActionPrototypes);
            uniqueActions.Add(GeneralChaseActionPrototype);
            uniqueActions.Add(GeneralTargetActionPrototype);
            uniqueActions.Add(Emote1ActionPrototype);
            uniqueActions.Add(Emote2ActionPrototype);
            uniqueActions.Add(Emote3ActionPrototype);
            uniqueActions.Add(Emote4ActionPrototype);
            uniqueActions.Add(ReviveActionPrototype);
            uniqueActions.Add(StunnedActionPrototype);
            uniqueActions.Add(DropActionPrototype);
            uniqueActions.Add(PickUpActionPrototype);

            m_AllActions = new List<Action>(uniqueActions.Count);

            int i = 0;
            foreach (var uniqueAction in uniqueActions)
            {
                uniqueAction.ActionID = new ActionID { ID = i };
                m_AllActions.Add(uniqueAction);
                i++;
            }
        }
    }
}
