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
            if (!IsServer)
            {
                this.enabled = false;
            }
            else
            {
                // listen for the client-connect event. This will only happen after
                // the ServerGNHLogic's approval-callback is done, meaning that if we get this event,
                // the client is officially allowed to be here.
                NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnected;

                // if any other players are already connected to us (i.e. they connected while we were 
                // in the login screen), give them player characters
                foreach (var connection in NetworkingManager.Singleton.ConnectedClientsList)
                {
                    SpawnPlayer(connection.ClientId);
                }
            }
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
