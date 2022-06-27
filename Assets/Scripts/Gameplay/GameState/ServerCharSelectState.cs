using System;
using System.Collections;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server specialization of Character Select game state.
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ServerCharSelectState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.CharSelect;
        public CharSelectData CharSelectData { get; private set; }

        Coroutine m_WaitToEndLobbyCoroutine;

        protected override void Awake()
        {
            base.Awake();
            CharSelectData = GetComponent<CharSelectData>();
        }

        void OnClientChangedSeat(ulong clientId, int newSeatIdx, bool lockedIn)
        {
            int idx = FindLobbyPlayerIdx(clientId);
            if (idx == -1)
            {
                throw new Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and cannot change seats! Shouldn't be here!");
            }

            if (CharSelectData.IsLobbyClosed.Value)
            {
                // The user tried to change their class after everything was locked in... too late! Discard this choice
                return;
            }

            if (newSeatIdx == -1)
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
                            CharSelectData.LobbyPlayers[idx].PlayerNumber,
                            CharSelectData.SeatState.Inactive);

                        // then early out
                        return;
                    }
                }
            }

            CharSelectData.LobbyPlayers[idx] = new CharSelectData.LobbyPlayerState(clientId,
                CharSelectData.LobbyPlayers[idx].PlayerName,
                CharSelectData.LobbyPlayers[idx].PlayerNumber,
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
                            CharSelectData.LobbyPlayers[i].PlayerNumber,
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
        void CloseLobbyIfReady()
        {
            foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
            {
                if (playerInfo.SeatState != CharSelectData.SeatState.LockedIn)
                    return; // nope, at least one player isn't locked in yet!
            }

            // everybody's ready at the same time! Lock it down!
            CharSelectData.IsLobbyClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveLobbyResults();

            // Delay a few seconds to give the UI time to react, then switch scenes
            m_WaitToEndLobbyCoroutine = StartCoroutine(WaitToEndLobby());
        }

        /// <summary>
        /// Cancels the process of closing the lobby, so that if a new player joins, they are able to chose a character.
        /// </summary>
        void CancelCloseLobby()
        {
            if (m_WaitToEndLobbyCoroutine != null)
            {
                StopCoroutine(m_WaitToEndLobbyCoroutine);
            }
            CharSelectData.IsLobbyClosed.Value = false;
        }

        void SaveLobbyResults()
        {
            foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
            {
                var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerInfo.ClientId);

                if (playerNetworkObject && playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
                {
                    // pass avatar GUID to PersistentPlayer
                    // it'd be great to simplify this with something like a NetworkScriptableObjects :(
                    persistentPlayer.NetworkAvatarGuidState.AvatarGuid.Value =
                        CharSelectData.AvatarConfiguration[playerInfo.SeatIdx].Guid.ToNetworkGuid();
                }
            }
        }

        IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            SceneLoaderWrapper.Instance.LoadScene("BossRoom", useNetworkSceneManager: true);
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            if (CharSelectData)
            {
                CharSelectData.OnClientChangedSeat -= OnClientChangedSeat;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                CharSelectData.OnClientChangedSeat += OnClientChangedSeat;

                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            // We need to filter out the event that are not a client has finished loading the scene
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;
            // When the client finishes loading the Lobby Map, we will need to Seat it
            SeatNewPlayer(sceneEvent.ClientId);
        }

        int GetAvailablePlayerNumber()
        {
            for (int possiblePlayerNumber = 0; possiblePlayerNumber < CharSelectData.k_MaxLobbyPlayers; ++possiblePlayerNumber)
            {
                if (IsPlayerNumberAvailable(possiblePlayerNumber))
                {
                    return possiblePlayerNumber;
                }
            }
            // we couldn't get a Player# for this person... which means the lobby is full!
            return -1;
        }

        bool IsPlayerNumberAvailable(int playerNumber)
        {
            bool found = false;
            foreach (CharSelectData.LobbyPlayerState playerState in CharSelectData.LobbyPlayers)
            {
                if (playerState.PlayerNumber == playerNumber)
                {
                    found = true;
                    break;
                }
            }

            return !found;
        }

        void SeatNewPlayer(ulong clientId)
        {
            // If lobby is closing and waiting to start the game, cancel to allow that new player to select a character
            if (CharSelectData.IsLobbyClosed.Value)
            {
                CancelCloseLobby();
            }

            SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            if (sessionPlayerData.HasValue)
            {
                var playerData = sessionPlayerData.Value;
                if (playerData.PlayerNumber == -1 || !IsPlayerNumberAvailable(playerData.PlayerNumber))
                {
                    // If no player num already assigned or if player num is no longer available, get an available one.
                    playerData.PlayerNumber = GetAvailablePlayerNumber();
                }
                if (playerData.PlayerNumber == -1)
                {
                    // Sanity check. We ran out of seats... there was no room!
                    throw new Exception($"we shouldn't be here, connection approval should have refused this connection already for client ID {clientId} and player num {playerData.PlayerNumber}");
                }

                CharSelectData.LobbyPlayers.Add(new CharSelectData.LobbyPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, CharSelectData.SeatState.Inactive));
                SessionManager<SessionPlayerData>.Instance.SetPlayerData(clientId, playerData);
            }
        }

        void OnClientDisconnectCallback(ulong clientId)
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

            if (!CharSelectData.IsLobbyClosed.Value)
            {
                // If the lobby is not already closing, close if the remaining players are all ready
                CloseLobbyIfReady();
            }
        }
    }
}
