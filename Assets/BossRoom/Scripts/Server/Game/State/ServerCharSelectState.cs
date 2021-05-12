using MLAPI;
using MLAPI.Messaging;
using System.Collections;
using MLAPI.Spawning;
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
        CharSelectData m_CharacterSelectData;

        [SerializeField]
        BossRoomPlayerRuntimeCollection m_BossRoomPlayers;

        public override GameState ActiveState => GameState.CharSelect;

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                m_CharacterSelectData.OnClientChangedSeat += OnClientChangedSeat;

                if (IsHost)
                {
                    // host doesn't get an OnClientConnected()
                    // and other clients could be connects from last game
                    // So look for any existing connections to do initial setup
                    var clients = NetworkManager.Singleton.ConnectedClientsList;
                    foreach (var networkClient in clients)
                    {
                        OnClientConnected(networkClient.ClientId);
                    }
                }
            }
        }

        void OnClientChangedSeat(ulong clientId, int newSeatIdx, bool lockedIn)
        {
            int idx = FindLobbyPlayerIdx(clientId);
            if (idx == -1)
            {
                //TODO-FIXME:MLAPI See note about MLAPI issue 745 in WaitToSeatNowPlayer.
                //while this workaround is in place, we must simply ignore these update requests from the client.
                //throw new System.Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and cannot change seats!");
                return;
            }

            if (m_CharacterSelectData.IsLobbyClosed.Value)
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
                foreach (CharSelectData.LobbyPlayerState playerInfo in m_CharacterSelectData.LobbyPlayers)
                {
                    if (playerInfo.ClientId != clientId && playerInfo.SeatIdx == newSeatIdx && playerInfo.SeatState == CharSelectData.SeatState.LockedIn)
                    {
                        // somebody already locked this choice in. Stop!
                        // Instead of granting lock request, change this player to Inactive state.
                        m_CharacterSelectData.LobbyPlayers[idx] = new CharSelectData.LobbyPlayerState(clientId,
                            m_CharacterSelectData.LobbyPlayers[idx].PlayerNum,
                            CharSelectData.SeatState.Inactive);

                        // then early out
                        return;
                    }
                }
            }

            m_CharacterSelectData.LobbyPlayers[idx] = new CharSelectData.LobbyPlayerState(clientId,
                m_CharacterSelectData.LobbyPlayers[idx].PlayerNum,
                lockedIn ? CharSelectData.SeatState.LockedIn : CharSelectData.SeatState.Active,
                newSeatIdx,
                Time.time);

            if (lockedIn)
            {
                // to help the clients visually keep track of who's in what seat, we'll "kick out" any other players
                // who were also in that seat. (Those players didn't click "Ready!" fast enough, somebody else took their seat!)
                for (int i = 0; i < m_CharacterSelectData.LobbyPlayers.Count; ++i)
                {
                    if (m_CharacterSelectData.LobbyPlayers[i].SeatIdx == newSeatIdx && i != idx)
                    {
                        // change this player to Inactive state.
                        m_CharacterSelectData.LobbyPlayers[i] = new CharSelectData.LobbyPlayerState(
                            m_CharacterSelectData.LobbyPlayers[i].ClientId,
                            m_CharacterSelectData.LobbyPlayers[i].PlayerNum,
                            CharSelectData.SeatState.Inactive);
                    }
                }
            }

            CloseLobbyIfReady();
        }

        /// <summary>
        /// Returns the index of a client in the master LobbyPlayer list, or -1 if not found
        /// </summary>
        int FindLobbyPlayerIdx(ulong clientId)
        {
            for (int i = 0; i < m_CharacterSelectData.LobbyPlayers.Count; ++i)
            {
                if (m_CharacterSelectData.LobbyPlayers[i].ClientId == clientId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Looks through all our connections and sees if everyone has locked in their choice;
        /// if so, we lock in the whole lobby, save state, and begin the transition to gameplay
        /// </summary>
        void CloseLobbyIfReady()
        {
            foreach (CharSelectData.LobbyPlayerState playerInfo in m_CharacterSelectData.LobbyPlayers)
            {
                if (playerInfo.SeatState != CharSelectData.SeatState.LockedIn)
                {
                    return; // nope, at least one player isn't locked in yet!
                }
            }

            // everybody's ready at the same time! Lock it down!
            m_CharacterSelectData.IsLobbyClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveLobbyResults();

            // Delay a few seconds to give the UI time to react, then switch scenes
            StartCoroutine(WaitToEndLobby());
        }

        /// <summary>
        /// Pass results from lobby to the players' components which carry over between scenes.
        /// </summary>
        void SaveLobbyResults()
        {
            foreach (CharSelectData.LobbyPlayerState playerInfo in m_CharacterSelectData.LobbyPlayers)
            {
                if (m_BossRoomPlayers.TryGetPlayer(playerInfo.ClientId, out BossRoomPlayer bossRoomPlayerData))
                {
                    if (bossRoomPlayerData.TryGetNetworkBehaviour(out NetworkCharacterTypeState networkCharacterTypeState) &&
                        networkCharacterTypeState)
                    {
                        networkCharacterTypeState.NetworkCharacterType =
                            m_CharacterSelectData.LobbySeatConfigurations[playerInfo.SeatIdx].Class;
                    }

                    if (bossRoomPlayerData.TryGetNetworkBehaviour(out NetworkAppearanceState networkAppearanceBehaviour) &&
                        networkAppearanceBehaviour)
                    {
                        networkAppearanceBehaviour.NetworkCharacterAppearance =
                            m_CharacterSelectData.LobbySeatConfigurations[playerInfo.SeatIdx].CharacterArtIdx;
                    }
                }
            }
        }

        IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("BossRoom");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
            if (m_CharacterSelectData)
            {
                m_CharacterSelectData.OnClientChangedSeat -= OnClientChangedSeat;
            }
        }

        void OnClientConnected(ulong clientId)
        {
            StartCoroutine(WaitToSeatNewPlayer(clientId));
        }

        IEnumerator WaitToSeatNewPlayer(ulong clientId)
        {
            //TODO-FIXME:MLAPI We are receiving NetworkVar updates too early on the client when doing this immediately on client connection,
            //causing the NetworkList of lobby players to get out of sync.
            //tracking MLAPI issue: https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/745
            //When issue is resolved, we should be able to call SeatNewPlayer directly in the client connection callback.
            yield return new WaitForSeconds(2.5f);
            SeatNewPlayer(clientId);
        }

        int GetAvailablePlayerNum()
        {
            for (int possiblePlayerNum = 0; possiblePlayerNum < CharSelectData.k_MaxLobbyPlayers; ++possiblePlayerNum)
            {
                bool found = false;
                foreach (CharSelectData.LobbyPlayerState playerState in m_CharacterSelectData.LobbyPlayers)
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

        void SeatNewPlayer(ulong clientId)
        {
            int playerNum = GetAvailablePlayerNum();
            if (playerNum == -1)
            {
                // we ran out of seats... there was no room!
                m_CharacterSelectData.FatalLobbyErrorClientRpc(CharSelectData.FatalLobbyError.LobbyFull,
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } });
                return;
            }

            // verifying that this clientID is indeed paired with a player object
            var networkObject = NetworkSpawnManager.GetPlayerNetworkObject(clientId);
            if (!networkObject)
            {
                Debug.LogError("Client could not be added to lobby!");
            }

            m_CharacterSelectData.LobbyPlayers.Add(new CharSelectData.LobbyPlayerState(clientId,
                playerNum,
                CharSelectData.SeatState.Inactive));
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            // clear this client's PlayerNumber and any associated visuals (so other players know they're gone).
            for (int i = 0; i < m_CharacterSelectData.LobbyPlayers.Count; ++i)
            {
                if (m_CharacterSelectData.LobbyPlayers[i].ClientId == clientId)
                {
                    m_CharacterSelectData.LobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
