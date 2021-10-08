using System;
using Unity.Collections;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Common data and RPCs for the CharSelect stage.
    /// </summary>
    public class CharSelectData : NetworkBehaviour
    {
        public enum SeatState : byte
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
        /// Describes one of the players in the lobby, and their current character-select status.
        /// </summary>
        public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
        {
            public ulong ClientId;

            private FixedPlayerName m_PlayerName; // I'm sad there's no 256Bytes fixed list :(
            // private byte[] m_PlayerName;

            public int PlayerNum; // this player's assigned "P#". (0=P1, 1=P2, etc.)
            public int SeatIdx; // the latest seat they were in. -1 means none
            public float LastChangeTime;

            public SeatState SeatState;


            public LobbyPlayerState(ulong clientId, string name, int playerNum, SeatState state, int seatIdx = -1, float lastChangeTime = 0)
            {
                ClientId = clientId;
                PlayerNum = playerNum;
                SeatState = state;
                SeatIdx = seatIdx;
                LastChangeTime = lastChangeTime;
                m_PlayerName = new FixedPlayerName();

                PlayerName = name;
            }

            public string PlayerName
            {
                get
                {
                    return m_PlayerName;
                }
                set
                {
                    m_PlayerName = value;
                }
            }
            // public string GetPlayerName()
            // {
            //
            // }
            // public void SetPlayerName(string value)
            // {
            //
            // }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref m_PlayerName);
                // serializer.SerializeValue(ref PlayerName);
                serializer.SerializeValue(ref PlayerNum);
                serializer.SerializeValue(ref SeatState);
                serializer.SerializeValue(ref SeatIdx);
                serializer.SerializeValue(ref LastChangeTime);
            }

            public bool Equals(LobbyPlayerState other)
            {
                return ClientId == other.ClientId &&
                       m_PlayerName.Equals(other.m_PlayerName) &&
                       PlayerNum == other.PlayerNum &&
                       SeatIdx == other.SeatIdx &&
                       LastChangeTime.Equals(other.LastChangeTime) &&
                       SeatState == other.SeatState;
            }

            public override bool Equals(object obj)
            {
                return obj is LobbyPlayerState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ClientId.GetHashCode();
                    hashCode = (hashCode * 397) ^ m_PlayerName.GetHashCode();
                    hashCode = (hashCode * 397) ^ PlayerNum;
                    hashCode = (hashCode * 397) ^ SeatIdx;
                    hashCode = (hashCode * 397) ^ LastChangeTime.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int) SeatState;
                    return hashCode;
                }
            }
        }

        private NetworkList<LobbyPlayerState> m_LobbyPlayers;

        public Avatar[] AvatarConfiguration;

        private void Awake()
        {
            m_LobbyPlayers = new NetworkList<LobbyPlayerState>();
        }

        /// <summary>
        /// Current state of all players in the lobby.
        /// </summary>
        public NetworkList<LobbyPlayerState> LobbyPlayers { get { return m_LobbyPlayers; } }

        /// <summary>
        /// When this becomes true, the lobby is closed and in process of terminating (switching to gameplay).
        /// </summary>
        public NetworkVariable<bool> IsLobbyClosed { get; } = new NetworkVariable<bool>(false);

        /// <summary>
        /// Client notification when the server has assigned this client a player Index (from 0 to 7);
        /// UI uses this tell whether we are "P1", "P2", etc. in the char-select UI
        /// </summary>
        public event Action<int> OnAssignedPlayerNumber;

        /// <summary>
        /// RPC to tell a client which slot in the char-gen screen they will be using.
        /// </summary>
        /// <param name="idx">Index on the UI screen, starting at 0 for the first slot</param>
        [ClientRpc]
        public void AssignPlayerNumberClientRpc(int idx)
        {
            OnAssignedPlayerNumber?.Invoke(idx);
        }

        // /// <summary>
        // /// Client notification when the server has told us that we cannot participate.
        // /// (Client should display an appropriate error and terminate)
        // /// </summary>
        // public event Action<FatalLobbyError> OnFatalLobbyError;

        // /// <summary>
        // /// RPC to tell a client that they cannot participate in the game due to a fatal error.
        // /// </summary>
        // [ClientRpc]
        // public void FatalLobbyErrorClientRpc(FatalLobbyError error, ClientRpcParams clientParams=default)
        // {
        //     OnFatalLobbyError?.Invoke(error);
        // }

        /// <summary>
        /// Server notification when a client requests a different lobby-seat, or locks in their seat choice
        /// </summary>
        public event Action<ulong, int, bool> OnClientChangedSeat;

        /// <summary>
        /// RPC to notify the server that a client has chosen a seat.
        /// </summary>
        [ServerRpc(RequireOwnership =false)]
        public void ChangeSeatServerRpc(ulong clientId, int seatIdx, bool lockedIn)
        {
            OnClientChangedSeat?.Invoke(clientId, seatIdx, lockedIn);
        }
    }
}
