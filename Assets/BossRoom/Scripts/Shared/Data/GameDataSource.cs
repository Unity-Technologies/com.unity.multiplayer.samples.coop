using System.Collections.Generic;
using UnityEngine;


namespace BossRoom
{
    public class GameDataSource : MonoBehaviour
    {
        [Tooltip("All CharacterClass data should be slotted in here")]
        public CharacterClass[] CharacterData;

        [Tooltip("All ActionDescription data should be slotted in here")]
        public ActionDescription[] ActionData;

        /// <summary>
        /// Contents of the CharacterData list, indexed by CharacterType for convenience. 
        /// </summary>
        public Dictionary<CharacterTypeEnum, CharacterClass> CharacterDataByType = new Dictionary<CharacterTypeEnum, CharacterClass>();

        /// <summary>
        /// Contents of the ActionData list, indexed by ActionType for convenience. 
        /// </summary>
        public Dictionary<ActionType, ActionDescription> ActionDataByType = new Dictionary<ActionType, ActionDescription>();

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

            foreach (CharacterClass data in CharacterData)
            {
                CharacterDataByType[data.CharacterType] = data;
            }

            foreach (ActionDescription data in ActionData)
            {
                ActionDataByType[data.ActionTypeEnum] = data;
            }
        }

    }
}

