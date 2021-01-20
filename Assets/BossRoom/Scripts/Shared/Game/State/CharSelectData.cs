using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using System;
using MLAPI.Messaging;

namespace BossRoom
{
    /// <summary>
    /// Common data and RPCs for the CharSelect stage. 
    /// </summary>
    public class CharSelectData : NetworkedBehaviour
    {
        #region public interface
        [Serializable]
        public enum SlotState
        {
            INACTIVE,
            ACTIVE,
            LOCKEDIN,
        }

        public const int MAX_LOBBY_PLAYERS = 8;

        /// <summary>
        /// Describes one of the eight lobby slots in the character-select screen.
        /// </summary>
        [Serializable]
        public struct CharSelectSlot
        {
            public CharacterTypeEnum Class;
            public bool IsMale;
            public SlotState State;

            public CharSelectSlot(CharacterTypeEnum Class, bool IsMale, SlotState State)
            {
                this.State = State;
                this.IsMale = IsMale;
                this.Class = Class;
            }
            public CharSelectSlot(string encoded)
            {
                string[] parts = encoded.Split(',');
                State = (SlotState)Enum.Parse(typeof(SlotState), parts[ 0 ]);
                IsMale = parts[ 1 ] == "1";
                Class = (CharacterTypeEnum)Enum.Parse(typeof(CharacterTypeEnum), parts[ 2 ]);
            }
            public string EncodeForMLAPI()
            {
                return State + "," + (IsMale ? "1" : "0") + "," + Class;
            }
        }

        /// <summary>
        /// Retrieves info about one of the slots in the character-select UI
        /// </summary>
        public CharSelectSlot GetCharSelectSlot(int idx)
        {
            if (m_CharSlots.Count <= idx)
                return new CharSelectSlot(CharacterTypeEnum.TANK, true, SlotState.INACTIVE);
            return new CharSelectSlot(m_CharSlots[ idx ]);
        }

        /// <summary>
        /// Set the networked-var info for a character slot. (Only the server can call this!)
        /// </summary>
        public void SetCharSelectSlot(int idx, CharSelectSlot slot)
        {
            while (m_CharSlots.Count <= idx)
                m_CharSlots.Add(new CharSelectSlot(CharacterTypeEnum.TANK, true, SlotState.INACTIVE).EncodeForMLAPI());
            m_CharSlots[ idx ] = slot.EncodeForMLAPI();
        }

        /// <summary>
        /// Notify clients that the lobby is all done and we'll be transitioning to gameplay soon.
        /// (Only the server can call this!)
        /// </summary>
        public void SetLobbyLocked()
        {
            m_Ready.Value = true;
        }

        /// <summary>
        /// Indicates whether the lobby is closed to further changes. 
        /// </summary>
        public bool IsLobbyLocked()
        {
            return m_Ready.Value;
        }

        /// <summary>
        /// Client code can register for this notification to be informed of changes to chargen slots
        /// </summary>
        public event Action<int, CharSelectSlot> OnCharSelectSlotChanged;

        /// <summary>
        /// Client code can register for this notification to know when all lobby participants are ready.
        /// The scene will be switched automatically a few seconds later, so this is just for UI visualization
        /// </summary>
        public event Action OnLobbyLockedIn;

        /// <summary>
        /// Client notification when the server has assigned this client a player Index (from 0 to 7);
        /// UI uses this tell whether we are "P1", "P2", etc. in the char-select UI
        /// </summary>
        public event Action<int> OnAssignedLobbyIndex;

        /// <summary>
        /// Server notification when a new client is available 
        /// </summary>
        public event Action<ulong> OnRequestLobbyIndex;

        /// <summary>
        /// Server notification when a client changes their char-gen state
        /// </summary>
        public event Action<ulong, CharacterTypeEnum, bool, SlotState> OnClientChangedSlot;
        #endregion


        #region Implementation details
        // Because MLAPI's NetworkedList cannot serialize arbitrary serializable
        // structures, we encode all the relevant info about a lobby-slot into a single
        // string, then reformat it into a struct whenever we want to work with it.
        // We hide this implementation detail from callers.
        private MLAPI.NetworkedVar.Collections.NetworkedList<string> m_CharSlots;
        private MLAPI.NetworkedVar.NetworkedVarBool m_Ready;

        private void Awake()
        {
            m_CharSlots = new MLAPI.NetworkedVar.Collections.NetworkedList<string>();
            m_Ready = new MLAPI.NetworkedVar.NetworkedVarBool(false);
        }

        public override void NetworkStart()
        {
            base.NetworkStart();
            m_CharSlots.OnListChanged += OnCharSlotsListChanged;
            m_Ready.OnValueChanged += OnReadyValueChanged;
        }

        private void OnCharSlotsListChanged(MLAPI.NetworkedVar.Collections.NetworkedListEvent<string> changeEvent)
        {
            OnCharSelectSlotChanged?.Invoke(changeEvent.index, new CharSelectSlot(changeEvent.value));
        }

        private void OnReadyValueChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                OnLobbyLockedIn?.Invoke();
            }
        }

        /// <summary>
        /// RPC to notify the server that a new client is in the lobby and is ready for a seat in the lobby.
        /// </summary>
        [ServerRPC(RequireOwnership = false)]
        public void RpcRequestLobbyIndex(ulong clientId)
        {
            OnRequestLobbyIndex?.Invoke(clientId);
        }

        /// <summary>
        /// RPC to notify the server that a client has chosen their class and/or locked in their choice.
        /// </summary>
        [ServerRPC(RequireOwnership = false)]
        public void RpcChangeSlot(ulong clientId, CharacterTypeEnum newClass, bool isMale, SlotState newState)
        {
            OnClientChangedSlot?.Invoke(clientId, newClass, isMale, newState);
        }

        /// <summary>
        /// RPC to tell a client which slot in the char-gen screen they will be using.
        /// </summary>
        /// <param name="movementTarget">Index on the UI screen, starting at 0 for the first slot</param>
        [ClientRPC]
        public void RpcAssignLobbyIndex(int idx)
        {
            OnAssignedLobbyIndex?.Invoke(idx);
        }
        #endregion

    }

}

