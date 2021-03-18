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

        private void Awake()
        {
            CharSelectData = GetComponent<CharSelectData>();
        }

        private void OnClientChangedSeat(ulong clientId, int newSeatIdx, bool lockedIn)
        {
            int idx = FindLobbyPlayerIdx(clientId);
            if (idx == -1)
                throw new System.Exception($"OnClientChangedSeat: client ID {clientId} is not a lobby player and cannot change seats!");

            if (CharSelectData.IsLobbyClosed.Value)
            {
                // The user tried to change their class after everything was locked in... too late! Discard this choice
                return;
            }

            // see if someone has already locked-in that seat! If so, too late... discard this choice
            foreach (CharSelectData.LobbyPlayerState playerInfo in CharSelectData.LobbyPlayers)
            {
                if (playerInfo.SeatIdx == newSeatIdx && playerInfo.SeatState == CharSelectData.SeatState.LockedIn)
                {
                    // yep, somebody already locked this choice in. Stop!
                    return;
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
            StartCoroutine(CoroEndLobby());
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

        private IEnumerator CoroEndLobby()
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
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                CharSelectData.OnClientChangedSeat += OnClientChangedSeat;

                if (IsHost)
                {
                    // host doesn't get an OnClientConnected()
                    // and other clients could be connects from last game
                    // So look for any existing connections to do intiial setup
                    var clients = NetworkManager.Singleton.ConnectedClientsList;
                    foreach (var net_cl in clients)
                    {
                        OnClientConnected(net_cl.ClientId);
                    }
                }
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // FIXME: here we work around another MLAPI bug when starting up scenes with in-scene networked objects.
            // We'd like to immediately give the new client a slot in our lobby, but if we try to send an RPC to the
            // client-side version of this scene NetworkObject, it will fail. The client's log will show
            //      "[MLAPI] ClientRPC message received for a non-existent object with id: 1. This message is lost."
            // If we wait a moment, the object will be assigned its ID (of 1) and everything will work. But there's no
            // notification to reliably tell us when the server and client are truly initialized.
            //
            // Add'l notes: I tried to work around this by having the newly-connected client send an "I'm ready" RPC to the
            // server, assuming that by the time the server received an RPC, it would be safe to respond. But the client
            // literally cannot send RPCs yet! If it sends one too quickly after connecting, the server gets a null-reference
            // exception. (Exception is in either MLAPI.NetworkBehaviour.InvokeServerRPCLocal() or
            // MLAPI.NetworkBehaviour.OnRemoteServerRPC, depending on whether we're in host mode or a standalone
            // client, respectively). This actually seems like a separate bug, but probably tied into the same problem.

            //      To repro the bug, comment out this line...
            StartCoroutine(CoroWorkAroundMlapiBug(clientId));
            //      ... and uncomment this one:
            //AssignNewLobbyIndex(clientId);
        }

        private IEnumerator CoroWorkAroundMlapiBug(ulong clientId)
        {
            var client = NetworkManager.Singleton.ConnectedClients[clientId];

            // for the host-mode client, a single frame of delay seems to be enough;
            // for networked connections, it often takes longer, so we wait a second.
            if (IsHost && clientId == NetworkManager.Singleton.LocalClientId)
                yield return new WaitForFixedUpdate();
            else
                yield return new WaitForSeconds(1);
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

            // this will be replaced with an auto-generated name
            string playerName = "Player" + (playerNum + 1);

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
