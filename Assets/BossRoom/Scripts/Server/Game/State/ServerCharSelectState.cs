using MLAPI;
using MLAPI.Messaging;
using System.Collections;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of Character Select game state.
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ServerCharSelectState : GameStateBehaviour
    {
        [SerializeField]
        NetworkObject m_LobbyPlayerPrefab;

        [SerializeField]
        PersistentPlayerRuntimeCollection m_PersistentPlayers;

        [SerializeField]
        LobbyPlayerRuntimeCollection m_LobbyPlayers;

        public override GameState ActiveState { get { return GameState.CharSelect; } }
        public CharSelectData CharSelectData { get; private set; }

        private ServerGameNetPortal m_ServerNetPortal;

        private void Awake()
        {
            CharSelectData = GetComponent<CharSelectData>();
            m_ServerNetPortal = GameObject.FindGameObjectWithTag("GameNetPortal").GetComponent<ServerGameNetPortal>();
        }

        private void OnClientChangedSeat(ulong clientId, int newSeatIdx, bool lockedIn)
        {
            int idx = FindLobbyPlayerIdx(clientId);
            if (idx == -1)
            {
                //TODO-FIXME:MLAPI See note about MLAPI issue 745 in WaitToSeatNowPlayer.
                //while this workaround is in place, we must simply ignore these update requests from the client.
                //throw new System.Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and cannot change seats!");
                return;
            }


            if (CharSelectData.IsLobbyClosed.Value)
            {
                // The user tried to change their class after everything was locked in... too late! Discard this choice
                return;
            }

            if ( newSeatIdx ==-1)
            {
                // we can't lock in with no seat
                lockedIn = false;
            }
            else
            {
                // see if someone has already locked-in that seat! If so, too late... discard this choice
                foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
                {
                    if (playerInfo.ClientId != clientId && playerInfo.SeatIdx == newSeatIdx && playerInfo.SeatState == CharSelectData.SeatState.LockedIn)
                    {
                        // somebody already locked this choice in. Stop!
                        // Instead of granting lock request, change this player to Inactive state.
                        CharSelectData.LobbyPlayers[idx] = new CharSelectData.LobbyPlayerState(clientId,
                            CharSelectData.LobbyPlayers[idx].PlayerName,
                            CharSelectData.LobbyPlayers[idx].PlayerNum,
                            CharSelectData.SeatState.Inactive);

                        // then early out
                        return;
                    }
                }
            }

            CharSelectData.LobbyPlayers[idx] = new CharSelectData.LobbyPlayerState(clientId,
                CharSelectData.LobbyPlayers[idx].PlayerName,
                CharSelectData.LobbyPlayers[idx].PlayerNum,
                lockedIn ? CharSelectData.SeatState.LockedIn : CharSelectData.SeatState.Active,
                newSeatIdx,
                Time.time);

            if (lockedIn)
            {
                // to help the clients visually keep track of who's in what seat, we'll "kick out" any other players
                // who were also in that seat. (Those players didn't click "Ready!" fast enough, somebody else took their seat!)
                for (int i = 0; i < CharSelectData.LobbyPlayers.Count; ++i)
                {
                    if (CharSelectData.LobbyPlayers[i].SeatIdx == newSeatIdx && i != idx)
                    {
                        // change this player to Inactive state.
                        CharSelectData.LobbyPlayers[i] = new CharSelectData.LobbyPlayerState(
                            CharSelectData.LobbyPlayers[i].ClientId,
                            CharSelectData.LobbyPlayers[i].PlayerName,
                            CharSelectData.LobbyPlayers[i].PlayerNum,
                            CharSelectData.SeatState.Inactive);
                    }
                }
            }

            CloseLobbyIfReady();
        }

        /// <summary>
        /// Returns the index of a client in the master LobbyPlayer list, or -1 if not found
        /// </summary>
        private int FindLobbyPlayerIdx(ulong clientId)
        {
            for (int i = 0; i < CharSelectData.LobbyPlayers.Count; ++i)
            {
                if (CharSelectData.LobbyPlayers[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Looks through all our connections and sees if everyone has locked in their choice;
        /// if so, we lock in the whole lobby, save state, and begin the transition to gameplay
        /// </summary>
        private void CloseLobbyIfReady()
        {
            foreach (var lobbyPlayer in m_LobbyPlayers.Items)
            {
                if (lobbyPlayer.NetworkLobbyState.ServerCharacterSelection.Value == -1)
                {
                    // found a player without a server-verified character selection; not ready to close lobby yet
                    return;
                }
            }

            /*foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
            {
                if (playerInfo.SeatState != CharSelectData.SeatState.LockedIn)
                    return; // nope, at least one player isn't locked in yet!
            }*/

            // everybody's ready at the same time! Lock it down!
            CharSelectData.IsLobbyClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveLobbyResults();

            // Delay a few seconds to give the UI time to react, then switch scenes
            StartCoroutine(WaitToEndLobby());
        }

        private void SaveLobbyResults()
        {
            foreach (var lobbyPlayer in m_LobbyPlayers.Items)
            {
                var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(lobbyPlayer.OwnerClientId);

                if (playerNetworkObject &&
                    playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
                {
                    // pass avatar GUID to PersistentPlayer
                    persistentPlayer.NetworkAvatarGuidState.AvatarGuidArray.Value =
                        CharSelectData.AvatarConfiguration[lobbyPlayer.NetworkLobbyState.ServerCharacterSelection.Value].Guid.ToByteArray();
                }
            }

            /*foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
            {
                var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerInfo.ClientId);

                if (playerNetworkObject &&
                    playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
                {
                    // pass avatar GUID to PersistentPlayer
                    persistentPlayer.NetworkAvatarGuidState.AvatarGuidArray.Value =
                        CharSelectData.AvatarConfiguration[playerInfo.SeatIdx].Guid.ToByteArray();
                }
            }*/
        }

        private IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            NetworkManager.SceneManager.SwitchScene("BossRoom");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                foreach (var persistentPlayer in m_PersistentPlayers.Items)
                {
                    PersistentPlayerAdded(persistentPlayer);
                }

                if (m_PersistentPlayers)
                {
                    m_PersistentPlayers.ItemAdded += PersistentPlayerAdded;
                }

                CharSelectData.ClientSeatConfirmed += ClientSeatConfirmed;

                /*NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                CharSelectData.OnClientChangedSeat += OnClientChangedSeat;

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.SceneManager.OnNotifyServerClientLoadedScene += OnNotifyServerClientLoadedScene;*/
            }
        }

        void PersistentPlayerAdded(PersistentPlayer persistentPlayer)
        {
            if (m_LobbyPlayers.TryGetPlayer(persistentPlayer.OwnerClientId, out LobbyPlayer lobbyPlayer))
            {
                // already added to dictionary
                return;
            }

            int playerNumber = -1;

            for (int i = 0; i < 8; i++)
            {
                var foundIndex = false;
                foreach (var value in m_LobbyPlayers.Items)
                {
                    if (value.NetworkLobbyState.LobbyNumber.Value == i)
                    {
                        foundIndex = true;
                        break;
                    }
                }

                if (foundIndex)
                {
                    continue;
                }

                playerNumber = i;
                break;
            }

            var clientID = persistentPlayer.OwnerClientId;
            StartCoroutine(WaitToSpawn(clientID, playerNumber));
        }

        IEnumerator WaitToSpawn(ulong clientID, int playerNumber)
        {
            yield return new WaitForSeconds(1f);

            var lobbyPlayerNetworkObject = Instantiate(m_LobbyPlayerPrefab);

            // spawn lobby players characters with destroyWithScene = true
            lobbyPlayerNetworkObject.SpawnWithOwnership(clientID, null, true);

            if (lobbyPlayerNetworkObject.TryGetComponent(out LobbyPlayer spawnedLobbyPlayer))
            {
                spawnedLobbyPlayer.NetworkLobbyState.LobbyNumber.Value = playerNumber;
            }
        }

        void ClientSeatConfirmed(ulong clientID, int seatID, bool isLockedIn)
        {
            if (!m_LobbyPlayers.TryGetPlayer(clientID, out LobbyPlayer lobbyPlayer))
            {
                return;
            }

            if (isLockedIn)
            {
                if (lobbyPlayer.NetworkLobbyState.ClientCharacterSelection.Value == seatID)
                {
                    // player is indeed holding this seat, confirm it
                    lobbyPlayer.NetworkLobbyState.ServerCharacterSelection.Value = seatID;
                }
            }
            else
            {
                lobbyPlayer.NetworkLobbyState.ServerCharacterSelection.Value = -1;
            }

            CloseLobbyIfReady();
        }

        public override void OnNetworkDespawn()
        {
            DeregisterCallbacks();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DeregisterCallbacks();
        }

        void DeregisterCallbacks()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnNotifyServerClientLoadedScene -= OnNotifyServerClientLoadedScene;
                }
            }
            if (CharSelectData)
            {
                CharSelectData.OnClientChangedSeat -= OnClientChangedSeat;
            }
        }

        private void OnNotifyServerClientLoadedScene(MLAPI.SceneManagement.SceneSwitchProgress progress, ulong clientId)
        {
            // When the client finishes loading the Lobby Map, we will need to Seat it
            SeatNewPlayer(clientId);
        }

        private void OnClientConnected(ulong clientId)
        {
            // When the client first connects to the server we will need to Seat it
            SeatNewPlayer(clientId);
        }

        private int GetAvailablePlayerNum()
        {
            for (int possiblePlayerNum = 0; possiblePlayerNum < CharSelectData.k_MaxLobbyPlayers; ++possiblePlayerNum)
            {
                bool found = false;
                foreach (CharSelectData.LobbyPlayerState playerState in CharSelectData.LobbyPlayers)
                {
                    if (playerState.PlayerNum == possiblePlayerNum)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return possiblePlayerNum;
                }
            }
            // we couldn't get a Player# for this person... which means the lobby is full!
            return -1;
        }

        private void SeatNewPlayer(ulong clientId)
        {
            int playerNum = GetAvailablePlayerNum();
            if (playerNum == -1)
            {
                // we ran out of seats... there was no room!
                CharSelectData.FatalLobbyErrorClientRpc(CharSelectData.FatalLobbyError.LobbyFull,
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } });
                return;
            }

            string playerName = m_ServerNetPortal.GetPlayerName(clientId,playerNum);
            CharSelectData.LobbyPlayers.Add(new CharSelectData.LobbyPlayerState(clientId, playerName, playerNum, CharSelectData.SeatState.Inactive));
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            // clear this client's PlayerNumber and any associated visuals (so other players know they're gone).
            for (int i = 0; i < CharSelectData.LobbyPlayers.Count; ++i)
            {
                if (CharSelectData.LobbyPlayers[i].ClientId == clientId)
                {
                    CharSelectData.LobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
