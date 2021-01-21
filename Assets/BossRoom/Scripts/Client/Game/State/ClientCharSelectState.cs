using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Client specialization of the Character Select game state. 
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ClientCharSelectState : GameStateBehaviour
    {
        /// <summary>
        /// Reference to the scene's state object so that UI can access state
        /// </summary>
        public static ClientCharSelectState Instance { get; private set; }

        public override GameState ActiveState { get { return GameState.CHARSELECT; } }
        public CharSelectData CharSelectData { get; private set; }
        public int CharIndex { get; private set; }

        private void Awake()
        {
            Instance = this;
            CharSelectData = GetComponent<CharSelectData>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CharSelectData.OnAssignedLobbyIndex -= OnAssignedCharIndex;
            if (Instance == this)
                Instance = null;
        }

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsClient)
            {
                this.enabled = false;
            }
            else
            {
                CharSelectData.OnAssignedLobbyIndex += OnAssignedCharIndex;
            }
        }

        public void ChangeSlot(CharacterTypeEnum newClass, bool newIsMale, CharSelectData.SlotState newState)
        {
            CharSelectData.InvokeServerRpc(CharSelectData.RpcChangeSlot,
                NetworkingManager.Singleton.LocalClientId,
                new CharSelectData.CharSelectSlot(newClass, newIsMale, newState));
       }

        private void OnAssignedCharIndex(int index)
        {
            CharIndex = index;
        }
    }
}
