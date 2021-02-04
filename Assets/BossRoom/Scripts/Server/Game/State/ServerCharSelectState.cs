using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of Character Select game state. 
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ServerCharSelectState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.CHARSELECT; } }
        public CharSelectData CharSelectData { get; private set; }

        private List<ulong> m_CharSlotClientIDs;

        private void Awake()
        {
            CharSelectData = GetComponent<CharSelectData>();
            CharSelectData.OnClientChangedSlot += OnClientChangedSlot;
            m_CharSlotClientIDs = new List<ulong>();
        }

        private void OnClientChangedSlot(ulong clientId, CharSelectData.CharSelectSlot newSlot)
        {
            if (CharSelectData.IsLobbyLocked.Value)
            {
                // The user tried to change their class after everything was locked in... too late! Discard this choice
                return;
            }

            int idx = FindClientIdx(clientId);
            if (idx == -1)
                throw new System.Exception("OnClientChangedSlot: unknown client ID " + clientId);

            CharSelectData.CharacterSlots[idx] = newSlot;
            if (newSlot.State == CharSelectData.SlotState.LOCKEDIN)
            {
                // it's possible that this is the last person we were waiting for. See if we're fully locked in!
                LockLobbyIfReady();
            }
        }

        /// <summary>
        /// Looks through all our connections and sees if everyone has locked in their choice;
        /// if so, we lock in the whole lobby, save state, and begin the transition to gameplay
        /// </summary>
        private void LockLobbyIfReady()
        {
            for (int i = 0; i < m_CharSlotClientIDs.Count; ++i)
            {
                if (MLAPI.NetworkingManager.Singleton.ConnectedClients.ContainsKey(m_CharSlotClientIDs[i]) &&
                    CharSelectData.CharacterSlots[i].State != CharSelectData.SlotState.LOCKEDIN)
                {
                    return; // this is a real player, and they are not ready to start, so we're done
                }
            }

            // everybody's ready at the same time! Lock it down!
            CharSelectData.IsLobbyLocked.Value = true;

            // remember our choices so the next scene can use the info
            LobbyResults lobbyResults = new LobbyResults();
            for (int i = 0; i < m_CharSlotClientIDs.Count; ++i)
            {
                if (MLAPI.NetworkingManager.Singleton.ConnectedClients.ContainsKey(m_CharSlotClientIDs[i]))
                {
                    var charSelectChoices = CharSelectData.CharacterSlots[i];
                    lobbyResults.Choices[m_CharSlotClientIDs[i]] = new LobbyResults.CharSelectChoice(charSelectChoices.Class, charSelectChoices.IsMale);
                }
            }
            GameStateRelay.SetRelayObject(lobbyResults);

            // Delay a few seconds to give the UI time to react, then switch scenes
            StartCoroutine(CoroEndLobby());
        }

        private IEnumerator CoroEndLobby()
        {
            yield return new WaitForSeconds(3);
            MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("DungeonTest");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkingManager.Singleton)
            {
                NetworkingManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkingManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
            CharSelectData.OnClientChangedSlot -= OnClientChangedSlot;
        }

        private int FindClientIdx(ulong clientId)
        {
            for (int i = 0; i < m_CharSlotClientIDs.Count; ++i)
            {
                if (m_CharSlotClientIDs[i] == clientId)
                    return i;
            }
            return -1;
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
                NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkingManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

                if (IsHost)
                {
                    // host doesn't get an OnClientConnected()
                    OnClientConnected(OwnerClientId);
                }
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // FIXME: here we work around another MLAPI bug when starting up scenes with in-scene networked objects.
            // We'd like to immediately give the new client a slot in our lobby, but if we try to send an RPC to the
            // client-side version of this scene NetworkedObject, it will fail. The client's log will show
            //      "[MLAPI] ClientRPC message received for a non-existent object with id: 1. This message is lost."
            // If we wait a moment, the object will be assigned its ID (of 1) and everything will work. But there's no
            // notification to reliably tell us when the server and client are truly initialized.
            // 
            // Add'l notes: I tried to work around this by having the newly-connected client send an "I'm ready" RPC to the
            // server, assuming that by the time the server received an RPC, it would be safe to respond. But the client
            // literally cannot send RPCs yet! If it sends one too quickly after connecting, the server gets a null-reference
            // exception. (Exception is in either MLAPI.NetworkedBehaviour.InvokeServerRPCLocal() or
            // MLAPI.NetworkedBehaviour.OnRemoteServerRPC, depending on whether we're in host mode or a standalone
            // client, respectively). This actually seems like a separate bug, but probably tied into the same problem.

            //      To repro the bug, comment out this line...
            StartCoroutine(CoroWorkAroundMlapiBug(clientId));
            //      ... and uncomment this one:
            //AssignNewLobbyIndex(clientId);
        }

        private IEnumerator CoroWorkAroundMlapiBug(ulong clientId)
        {
            var client = NetworkingManager.Singleton.ConnectedClients[clientId];

            // for the host-mode client, a single frame of delay seems to be enough;
            // for networked connections, it often takes longer, so we wait a second.
            if (IsHost && clientId == NetworkingManager.Singleton.LocalClientId)
                yield return new WaitForFixedUpdate();
            else
                yield return new WaitForSeconds(1);
            AssignNewLobbyIndex(clientId);
        }

        private void AssignNewLobbyIndex(ulong clientId)
        {
            int newClientIdx = -1;
            // see if any of the existing slots are for a client ID that's dead.
            // Note that we may find this new clientId is already in our list, if
            // "reuse client IDs" is enabled... and it is! This means that somebody
            // else was in the lobby, but then quit, and a new client got their ID.
            for (int i = 0; i < m_CharSlotClientIDs.Count; ++i)
            {
                if (m_CharSlotClientIDs[i] == clientId ||
                    !MLAPI.NetworkingManager.Singleton.ConnectedClients.ContainsKey(m_CharSlotClientIDs[i]))
                {
                    // it's a dead slot; reuse it!
                    m_CharSlotClientIDs[i] = clientId;
                    newClientIdx = i;
                    break;
                }
            }

            if (newClientIdx == -1 && m_CharSlotClientIDs.Count < CharSelectData.k_MaxLobbyPlayers)
            {
                // all existing slots are in use; get a new one, if there's room...
                newClientIdx = m_CharSlotClientIDs.Count;
                m_CharSlotClientIDs.Add(clientId);
            }

            if (newClientIdx == -1)
            {
                // there was no room!
                CharSelectData.InvokeClientRpcOnClient(CharSelectData.RpcFatalLobbyError, clientId, CharSelectData.FatalLobbyError.LOBBY_FULL, "MLAPI_INTERNAL");
            }
            else
            {
                CharSelectData.InvokeClientRpcOnClient(CharSelectData.RpcAssignLobbyIndex, clientId, newClientIdx, "MLAPI_INTERNAL");
            }
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            // find this player's old slot and set their visuals to inactive, so other players know they're gone.
            int idx = FindClientIdx(clientId);
            if (idx != -1)
            {
                CharSelectData.CharacterSlots[idx] = new CharSelectData.CharSelectSlot(CharSelectData.SlotState.INACTIVE);
            }
        }
    }

}

