using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;
using System;
using MLAPI;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of core BossRoom game logic. 
    /// </summary>
    public class ServerBossRoomState : GameStateBehaviour
    {
        [SerializeField] private NetworkedObject PlayerPrefab;

        public override GameState ActiveState { get { return GameState.BOSSROOM; } }


        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer && !IsHost)
            {
                this.enabled = false;
            }
            else
            {
                // listen for the client-connect event. This will only happen after
                // the ServerGNHLogic's approval-callback is done, meaning that if we get this event,
                // the client is officially allowed to be here.
                MLAPI.NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnected;

                if (IsHost)
                {
                    // start local "host" character too!
                    SpawnPlayer(MLAPI.NetworkingManager.Singleton.LocalClientId);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
        }

        private void OnClientConnected(ulong clientId)
        {
            // FIXME: this is a work-around for an MLAPI timing problem which happens semi-reliably; 
            // when it happens, it generates the same errors and has the same behavior as this: 
            //      https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/328
            // We can't use the workaround suggested there, which is to avoid using MLAPI's scene manager.
            // Instead, we wait a bit for MLAPI to get its state organized, because we can't safely create entities in OnClientConnected().
            // (Note: on further explortation, I think this is due to some sort of scene-loading synchronization: the new client is briefly 
            // "in" the lobby screen, but has already told the server it's in the game scene. Or something similar.)
            //
            // Note: this workaround doesn't help us when the client connects during the host's scene-load (i.e. when the Host is in char-gen 
            // screen and gets a new connection) and the ServerBossRoomState doesn't exist yet. That's an unrelated problem, and not an 
            // MLAPI issue! ... But it generates the same error message ("Cannot find pending soft sync object. Is the projects the same?") 
            // so wanted to mention it.
            StartCoroutine(CoroSpawnPlayer(clientId));
        }

        private IEnumerator CoroSpawnPlayer(ulong clientId)
        {
            yield return new WaitForSeconds(1);
            SpawnPlayer(clientId);
        }

        private void SpawnPlayer(ulong clientId)
        {
            var NewPlayer = Instantiate(PlayerPrefab);
            NewPlayer.SpawnAsPlayerObject(clientId);
        }
    }
}
