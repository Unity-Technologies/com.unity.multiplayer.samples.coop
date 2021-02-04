using MLAPI;
using MLAPI.Messaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace BossRoom
{
    /// <summary>
    /// Common data and RPCs for the CharSelect stage. 
    /// </summary>
    public class CharSelectData : NetworkedBehaviour
    {
        public enum SlotState
        {
            Inactive,
            Active,
            LockedIn,
        }

        public enum FatalLobbyError
        {
            LobbyFull,
        }

        public const int k_MaxLobbyPlayers = 8;

        /// <summary>
        /// Describes one of the eight lobby slots in the character-select screen.
        /// </summary>
        public struct CharSelectSlot : MLAPI.Serialization.IBitWritable
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

            public CharSelectSlot(SlotState State)
            {
                this.State = State;
                this.IsMale = true;
                this.Class = CharacterTypeEnum.Tank;
            }

            public void Read(Stream stream)
            {
                using (var reader = MLAPI.Serialization.Pooled.PooledBitReader.Get(stream))
                {
                    Class = (CharacterTypeEnum)reader.ReadInt16();
                    IsMale = reader.ReadBool();
                    State = (SlotState)reader.ReadByte();
                }
            }

            public void Write(Stream stream)
            {
                using (var writer = MLAPI.Serialization.Pooled.PooledBitWriter.Get(stream))
                {
                    writer.WriteInt16((short)Class);
                    writer.WriteBool(IsMale);
                    writer.WriteByte((byte)State);
                }
            }
        }

        /// <summary>
        /// Current state of each of the seats in the lobby.
        /// </summary>
        public MLAPI.NetworkedVar.Collections.NetworkedList<CharSelectSlot> CharacterSlots { get; private set; }

        /// <summary>
        /// When this becomes true, the lobby is closed and in process of terminating (switching to gameplay).
        /// </summary>
        public MLAPI.NetworkedVar.NetworkedVarBool IsLobbyLocked { get; } = new MLAPI.NetworkedVar.NetworkedVarBool(false);

        /// <summary>
        /// Client notification when the server has assigned this client a player Index (from 0 to 7);
        /// UI uses this tell whether we are "P1", "P2", etc. in the char-select UI
        /// </summary>
        public event Action<int> OnAssignedLobbyIndex;

        /// <summary>
        /// RPC to tell a client which slot in the char-gen screen they will be using.
        /// </summary>
        /// <param name="idx">Index on the UI screen, starting at 0 for the first slot</param>
        [ClientRPC]
        public void RpcAssignLobbyIndex(int idx)
        {
            OnAssignedLobbyIndex?.Invoke(idx);
        }

        /// <summary>
        /// Client notification when the server has told us that we cannot participate.
        /// (Client should display an appropriate error and terminate)
        /// </summary>
        public event Action<FatalLobbyError> OnFatalLobbyError;

        /// <summary>
        /// RPC to tell a client that they cannot participate in the game due to a fatal error.
        /// </summary>
        [ClientRPC]
        public void RpcFatalLobbyError(FatalLobbyError error)
        {
            OnFatalLobbyError?.Invoke(error);
        }

        /// <summary>
        /// Server notification when a client requests changes to their char-gen state
        /// </summary>
        public event Action<ulong, CharSelectSlot> OnClientChangedSlot;

        /// <summary>
        /// RPC to notify the server that a client has chosen their class and/or locked in their choice.
        /// </summary>
        [ServerRPC(RequireOwnership = false)]
        public void RpcChangeSlot(ulong clientId, CharSelectSlot newSlot)
        {
            OnClientChangedSlot?.Invoke(clientId, newSlot);
        }

        private void Awake()
        {
            List<CharSelectSlot> initialList = new List<CharSelectSlot>();
            for (int i = 0; i < k_MaxLobbyPlayers; ++i)
            {
                initialList.Add(new CharSelectSlot(SlotState.Inactive));
            }

            // initialize the char-slots list with all the slots it will ever have
            CharacterSlots = new MLAPI.NetworkedVar.Collections.NetworkedList<CharSelectSlot>(initialList);
        }

    }

}
