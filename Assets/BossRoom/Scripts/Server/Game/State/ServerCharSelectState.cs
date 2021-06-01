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
            StartCoroutine(WaitToEndLobby());
        }

        private void SaveLobbyResults()
        {
            LobbyResults lobbyResults = new LobbyResults();
            foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
            {
                lobbyResults.Choices[playerInfo.ClientId] = new LobbyResults.CharSelectChoice(playerInfo.PlayerNum,
                    CharSelectData.LobbySeatConfigurations[playerInfo.SeatIdx].Class,
                    CharSelectData.LobbySeatConfigurations[playerInfo.SeatIdx].CharacterArtIdx);
            }
            GameStateRelay.SetRelayObject(lobbyResults);
        }

        private IEnumerator WaitToEndLobby()
        {
            yield return new WaitForSeconds(3);
            NetworkManager.SceneManager.SwitchScene("BossRoom");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
            if (CharSelectData)
            {
                CharSelectData.OnClientChangedSeat -= OnClientChangedSeat;
            }
        }

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                CharSelectData.OnClientChangedSeat += OnClientChangedSeat;

                // Cosmin: Question? Should OnNotifyServerClientLoadedScene delegate also cover
                // When a new client joins in? If yes, then this we will become obsolete for our case here
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                // NSS: Suggested Fix, use the OnNotifyServerClientLoadedScene to let you know that the client
                // is initialized, all scene NetworkObjects have been loaded, and can be considered "ready to send and receive messages".
                NetworkManager.Singleton.SceneManager.OnNotifyServerClientLoadedScene += SceneManager_OnNotifyServerClientLoadedScene;

                //if (IsHost)
                //{
                //    // host doesn't get an OnClientConnected()
                //    // and other clients could be connects from last game
                //    // So look for any existing connections to do intiial setup
                //    var clients = NetworkManager.Singleton.ConnectedClientsList;
                //    foreach (var net_cl in clients)
                //    {
                //        OnClientConnected(net_cl.ClientId);
                //    }
                //}
            }
        }

        // NSS: Suggested Fix
        private void SceneManager_OnNotifyServerClientLoadedScene(MLAPI.SceneManagement.SceneSwitchProgress progress, ulong clientId)
        {
            SeatNewPlayer(clientId);
        }

        private void OnClientConnected(ulong clientId)
        {

            // Cosmin: We no longer need a delayed/coroutine to handle this, now that we have OnNotifyServerClientLoadedScene
            // Implemented at the SDK level, which should correctly cover for this workaround
            //StartCoroutine(WaitToSeatNewPlayer(clientId));
            SeatNewPlayer(clientId);
        }

        //private IEnumerator WaitToSeatNewPlayer(ulong clientId)
        //{
        //    //TODO-FIXME:MLAPI We are receiving NetworkVar updates too early on the client when doing this immediately on client connection,
        //    //causing the NetworkList of lobby players to get out of sync.
        //    //tracking MLAPI issue: https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/745
        //    //When issue is resolved, we should be able to call SeatNewPlayer directly in the client connection callback.
        //    yield return new WaitForSeconds(2.5f);
        //    SeatNewPlayer(clientId);
        //}

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
