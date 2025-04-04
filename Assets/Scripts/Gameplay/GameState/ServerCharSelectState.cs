using System;
using System.Collections;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Server specialization of Character Select game state.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks), typeof(NetworkCharSelection))]
    public class ServerCharSelectState : GameStateBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        public override GameState ActiveState => GameState.CharSelect;
        public NetworkCharSelection networkCharSelection { get; private set; }

        Coroutine m_WaitToEndSessionCoroutine;

        [Inject]
        ConnectionManager m_ConnectionManager;

        protected override void Awake()
        {
            base.Awake();
            networkCharSelection = GetComponent<NetworkCharSelection>();

            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_NetcodeHooks)
            {
                m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                m_NetcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }
        }

        void OnClientChangedSeat(ulong clientId, int newSeatIdx, bool lockedIn)
        {
            int idx = FindSessionPlayerIdx(clientId);
            if (idx == -1)
            {
                throw new Exception($"OnClientChangedSeat: client ID {clientId} is not a Session player and cannot change seats! Shouldn't be here!");
            }

            if (networkCharSelection.IsSessionClosed.Value)
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
                foreach (NetworkCharSelection.SessionPlayerState playerInfo in networkCharSelection.sessionPlayers)
                {
                    if (playerInfo.ClientId != clientId && playerInfo.SeatIdx == newSeatIdx && playerInfo.SeatState == NetworkCharSelection.SeatState.LockedIn)
                    {
                        // somebody already locked this choice in. Stop!
                        // Instead of granting lock request, change this player to Inactive state.
                        networkCharSelection.sessionPlayers[idx] = new NetworkCharSelection.SessionPlayerState(clientId,
                            networkCharSelection.sessionPlayers[idx].PlayerName,
                            networkCharSelection.sessionPlayers[idx].PlayerNumber,
                            NetworkCharSelection.SeatState.Inactive);

                        // then early out
                        return;
                    }
                }
            }

            networkCharSelection.sessionPlayers[idx] = new NetworkCharSelection.SessionPlayerState(clientId,
                networkCharSelection.sessionPlayers[idx].PlayerName,
                networkCharSelection.sessionPlayers[idx].PlayerNumber,
                lockedIn ? NetworkCharSelection.SeatState.LockedIn : NetworkCharSelection.SeatState.Active,
                newSeatIdx,
                Time.time);

            if (lockedIn)
            {
                // to help the clients visually keep track of who's in what seat, we'll "kick out" any other players
                // who were also in that seat. (Those players didn't click "Ready!" fast enough, somebody else took their seat!)
                for (int i = 0; i < networkCharSelection.sessionPlayers.Count; ++i)
                {
                    if (networkCharSelection.sessionPlayers[i].SeatIdx == newSeatIdx && i != idx)
                    {
                        // change this player to Inactive state.
                        networkCharSelection.sessionPlayers[i] = new NetworkCharSelection.SessionPlayerState(
                            networkCharSelection.sessionPlayers[i].ClientId,
                            networkCharSelection.sessionPlayers[i].PlayerName,
                            networkCharSelection.sessionPlayers[i].PlayerNumber,
                            NetworkCharSelection.SeatState.Inactive);
                    }
                }
            }

            CloseSessionIfReady();
        }

        /// <summary>
        /// Returns the index of a client in the master SessionPlayer list, or -1 if not found
        /// </summary>
        int FindSessionPlayerIdx(ulong clientId)
        {
            for (int i = 0; i < networkCharSelection.sessionPlayers.Count; ++i)
            {
                if (networkCharSelection.sessionPlayers[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Looks through all our connections and sees if everyone has locked in their choice;
        /// if so, we lock in the whole Session, save state, and begin the transition to gameplay
        /// </summary>
        void CloseSessionIfReady()
        {
            foreach (NetworkCharSelection.SessionPlayerState playerInfo in networkCharSelection.sessionPlayers)
            {
                if (playerInfo.SeatState != NetworkCharSelection.SeatState.LockedIn)
                    return; // nope, at least one player isn't locked in yet!
            }

            // everybody's ready at the same time! Lock it down!
            networkCharSelection.IsSessionClosed.Value = true;

            // remember our choices so the next scene can use the info
            SaveSessionResults();

            // Delay a few seconds to give the UI time to react, then switch scenes
            m_WaitToEndSessionCoroutine = StartCoroutine(WaitToEndSession());
        }

        /// <summary>
        /// Cancels the process of closing the Session, so that if a new player joins, they are able to choose a character.
        /// </summary>
        void CancelCloseSession()
        {
            if (m_WaitToEndSessionCoroutine != null)
            {
                StopCoroutine(m_WaitToEndSessionCoroutine);
            }
            networkCharSelection.IsSessionClosed.Value = false;
        }

        void SaveSessionResults()
        {
            foreach (NetworkCharSelection.SessionPlayerState playerInfo in networkCharSelection.sessionPlayers)
            {
                var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerInfo.ClientId);

                if (playerNetworkObject && playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
                {
                    // pass avatar GUID to PersistentPlayer
                    // it'd be great to simplify this with something like a NetworkScriptableObjects :(
                    persistentPlayer.NetworkAvatarGuidState.AvatarGuid.Value =
                        networkCharSelection.AvatarConfiguration[playerInfo.SeatIdx].Guid.ToNetworkGuid();
                }
            }
        }

        IEnumerator WaitToEndSession()
        {
            yield return new WaitForSeconds(3);
            SceneLoaderWrapper.Instance.LoadScene("BossRoom", useNetworkSceneManager: true);
        }

        void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            if (networkCharSelection)
            {
                networkCharSelection.OnClientChangedSeat -= OnClientChangedSeat;
            }
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
                networkCharSelection.OnClientChangedSeat += OnClientChangedSeat;

                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            // We need to filter out the event that are not a client has finished loading the scene
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;
            // When the client finishes loading the Session Map, we will need to Seat it
            SeatNewPlayer(sceneEvent.ClientId);
        }

        int GetAvailablePlayerNumber()
        {
            for (int possiblePlayerNumber = 0; possiblePlayerNumber < m_ConnectionManager.MaxConnectedPlayers; ++possiblePlayerNumber)
            {
                if (IsPlayerNumberAvailable(possiblePlayerNumber))
                {
                    return possiblePlayerNumber;
                }
            }
            // we couldn't get a Player# for this person... which means the Session is full!
            return -1;
        }

        bool IsPlayerNumberAvailable(int playerNumber)
        {
            bool found = false;
            foreach (NetworkCharSelection.SessionPlayerState playerState in networkCharSelection.sessionPlayers)
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
            // If Session is closing and waiting to start the game, cancel to allow that new player to select a character
            if (networkCharSelection.IsSessionClosed.Value)
            {
                CancelCloseSession();
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

                networkCharSelection.sessionPlayers.Add(new NetworkCharSelection.SessionPlayerState(clientId, playerData.PlayerName, playerData.PlayerNumber, NetworkCharSelection.SeatState.Inactive));
                SessionManager<SessionPlayerData>.Instance.SetPlayerData(clientId, playerData);
            }
        }

        void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType == ConnectionEvent.ClientDisconnected)
            {
                // clear this client's PlayerNumber and any associated visuals (so other players know they're gone).
                for (int i = 0; i < networkCharSelection.sessionPlayers.Count; ++i)
                {
                    if (networkCharSelection.sessionPlayers[i].ClientId == connectionEventData.ClientId)
                    {
                        networkCharSelection.sessionPlayers.RemoveAt(i);
                        break;
                    }
                }

                if (!networkCharSelection.IsSessionClosed.Value)
                {
                    // If the Session is not already closing, close if the remaining players are all ready
                    CloseSessionIfReady();
                }
            }
        }
    }
}
