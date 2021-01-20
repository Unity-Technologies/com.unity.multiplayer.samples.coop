using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;
using MLAPI;

namespace BossRoom.Client
{
    /// <summary>
    /// Client specialization of the Character Select game state. 
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ClientCharSelectState : GameStateBehaviour
    {
        /// <summary>
        /// Reference to the scene's state object so that UI can access state
        /// </summary>
        public static ClientCharSelectState Instance;

        public override GameState ActiveState { get { return GameState.CHARSELECT; } }
        public CharSelectData CharSelectData { get; private set; }
        public int CharIndex { get; private set; }

        private void Awake()
        {
            Instance = this;
            CharSelectData = GetComponent<CharSelectData>();
        }

        private void OnDestroy()
        {
            CharSelectData.OnAssignedLobbyIndex -= OnAssignedCharIndex;
            if (Instance == this)
                Instance = null;
        }

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsClient)
            {
                this.enabled = false;
            }
            else
            {
                CharSelectData.OnAssignedLobbyIndex += OnAssignedCharIndex;
            }
        }

        public void ChangeSlot(CharacterTypeEnum newClass, bool newIsMale, CharSelectData.SlotState newState)
        {
            CharSelectData.InvokeServerRpc(CharSelectData.RpcChangeSlot,
                NetworkingManager.Singleton.LocalClientId,
                newClass, newIsMale, newState);
        }


        /*
        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsClient)
            {
                this.enabled = false;
            }
            else
            {
                // Work around another MLAPI timing bug. (Similar to other reported timing bug, but is *probably* not the same?)
                // Because this component is attached to an in-scene NetworkedObject and not a dynamically-created one,
                // our NetworkStart() is called before the server has finished initializing the server-side version of us.
                // (We are fully initialized here on the client, but not the server.) If we try to call RPC functions too
                // quickly, the server will get a null-reference-exception in MLAPI.NetworkedBehaviour.InvokeServerRPCLocal()
                // or MLAPI.NetworkedBehaviour.OnRemoteServerRPC (depending on whether we're in host mode or a separate
                // client, respectively). 
                //
                // (This is also why the server can't just use its OnClientConnected() to immediately assign us a lobby slot:
                // sending client RPCs immediately after learning about a client is too soon, and always fails.)
                //
                // There doesn't seem to be any way to be notified of when we are REALLY ready to send/receive RPCs.
                // In testing, waiting a single physics frame seems to be sufficient... but since its a problem on the SERVER
                // and we're delaying in the client, there is no reason to believe a single frame will always be enough time.
                //
                // To repro this bug, simply comment out the line indicated in CoroRequestLobbyIndex() and then host a game.
                StartCoroutine(CoroRequestLobbyIndex());
            }
        }

        private IEnumerator CoroRequestLobbyIndex()
        {
            // COMMENT OUT THE NEXT LINE TO REPRO BUG
            yield return new WaitForFixedUpdate();
             
            NetData.InvokeServerRpc(NetData.RpcRequestLobbyIndex, NetworkingManager.Singleton.LocalClientId);
            yield break;
        }
        */

        private void OnAssignedCharIndex(int index)
        {
            Debug.Log("We've been assigned index #" + index);
            CharIndex = index;
        }
    }
}
