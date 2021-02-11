using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Common data and RPCs for the CharSelect stage. 
    /// </summary>
    public class CharSelectData : NetworkedBehaviour
    {
        public enum SeatState
        {
            Inactive,
            Active,
            LockedIn,
        }

        public enum FatalLobbyError
        {
            LobbyFull,
        }

        public struct LobbySeatConfiguration
        {
            public CharacterTypeEnum Class;
            public int CharacterArtIdx;
            public LobbySeatConfiguration(CharacterTypeEnum charClass, int artIdx)
            {
                Class = charClass;
                CharacterArtIdx = artIdx;
            }
        }

        /// <summary>
        /// Each "seat" in the lobby has an assigned class/gender, and this tells us what it is
        /// </summary>
        public static LobbySeatConfiguration[] k_LobbySeatConfigurations = new LobbySeatConfiguration[]
        {
            new LobbySeatConfiguration(CharacterTypeEnum.Tank, 0),
            new LobbySeatConfiguration(CharacterTypeEnum.Archer, 2),
            new LobbySeatConfiguration(CharacterTypeEnum.Mage, 4),
            new LobbySeatConfiguration(CharacterTypeEnum.Rogue, 6),
            new LobbySeatConfiguration(CharacterTypeEnum.Tank, 1),
            new LobbySeatConfiguration(CharacterTypeEnum.Archer, 3),
            new LobbySeatConfiguration(CharacterTypeEnum.Mage, 5),
            new LobbySeatConfiguration(CharacterTypeEnum.Rogue, 7),
        };

        public const int k_MaxLobbyPlayers = 8;

        /// <summary>
        /// Describes one of players in the lobby, and their current character-select status.
        /// </summary>
        public struct LobbyPlayerState : MLAPI.Serialization.IBitWritable
        {
            public ulong ClientId;
            public string PlayerName;
            public int PlayerNum; // this player's assigned "P#". (0=P1, 1=P2, etc.)
            public int SeatIdx; // the latest seat they were in. -1 means none
            public SeatState SeatState;
            public float LastChangeTime;

            public LobbyPlayerState(ulong clientId, string name, int playerNum, SeatState state, int seatIdx = -1, float lastChangeTime = 0)
            {
                ClientId = clientId;
                PlayerName = name;
                PlayerNum = playerNum;
                SeatState = state;
                SeatIdx = seatIdx;
                LastChangeTime = lastChangeTime;
            }

            public void Read(Stream stream)
            {
                using (var reader = MLAPI.Serialization.Pooled.PooledBitReader.Get(stream))
                {
                    ClientId = reader.ReadUInt64();
                    PlayerName = reader.ReadString().ToString();
                    PlayerNum = reader.ReadInt16();
                    SeatState = (SeatState)reader.ReadInt16();
                    SeatIdx = reader.ReadInt16();
                    LastChangeTime = reader.ReadSingle();
                }
            }

            public void Write(Stream stream)
            {
                using (var writer = MLAPI.Serialization.Pooled.PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64(ClientId);
                    writer.WriteString(PlayerName);
                    writer.WriteInt16((short)PlayerNum);
                    writer.WriteInt16((short)SeatState);
                    writer.WriteInt16((short)SeatIdx);
                    writer.WriteSingle(LastChangeTime);
                }
            }
        }

        /// <summary>
        /// Current state of all players in the lobby.
        /// </summary>
        public MLAPI.NetworkedVar.Collections.NetworkedList<LobbyPlayerState> LobbyPlayers { get; } = new MLAPI.NetworkedVar.Collections.NetworkedList<LobbyPlayerState>();

        /// <summary>
        /// When this becomes true, the lobby is closed and in process of terminating (switching to gameplay).
        /// </summary>
        public MLAPI.NetworkedVar.NetworkedVarBool IsLobbyClosed { get; } = new MLAPI.NetworkedVar.NetworkedVarBool(false);

        /// <summary>
        /// Client notification when the server has assigned this client a player Index (from 0 to 7);
        /// UI uses this tell whether we are "P1", "P2", etc. in the char-select UI
        /// </summary>
        public event Action<int> OnAssignedPlayerNumber;

        /// <summary>
        /// RPC to tell a client which slot in the char-gen screen they will be using.
        /// </summary>
        /// <param name="idx">Index on the UI screen, starting at 0 for the first slot</param>
        [ClientRPC]
        public void RpcAssignPlayerNumber(int idx)
        {
            OnAssignedPlayerNumber?.Invoke(idx);
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
        /// Server notification when a client requests a different lobby-seat, or locks in their seat choice
        /// </summary>
        public event Action<ulong, int, bool> OnClientChangedSeat;

        /// <summary>
        /// RPC to notify the server that a client has chosen a seat.
        /// </summary>
        [ServerRPC(RequireOwnership = false)]
        public void RpcChangeSeat(ulong clientId, int seatIdx, bool lockedIn)
        {
            OnClientChangedSeat?.Invoke(clientId, seatIdx, lockedIn);
        }
    }
}
