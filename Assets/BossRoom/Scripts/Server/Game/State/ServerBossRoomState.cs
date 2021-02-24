using MLAPI;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of core BossRoom game logic.
    /// </summary>
    public class ServerBossRoomState : GameStateBehaviour
    {
        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkingManager's list of prefabs!")]
        private NetworkedObject m_PlayerPrefab;

        // note: this is temporary, for testing!
        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkingManager's list of prefabs!")]
        private NetworkedObject m_EnemyPrefab;

        // note: this is temporary, for testing!
        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkingManager's list of prefabs!")]
        private NetworkedObject m_BossPrefab;

        [SerializeField]
        [Tooltip("Set what sort of character class gets created for players by default.")]
        private CharacterTypeEnum m_DefaultPlayerType = CharacterTypeEnum.Tank;

        [SerializeField]
        [Tooltip("Set the default Player Appearance (value between 0-7)")]
        private int m_DefaultPlayerAppearance = 7;

        public override GameState ActiveState { get { return GameState.BossRoom; } }

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
            var newPlayer = Instantiate(m_PlayerPrefab);
            var netState = newPlayer.GetComponent<NetworkCharacterState>();
            netState.CharacterType.Value = m_DefaultPlayerType;
            netState.CharacterAppearance.Value = m_DefaultPlayerAppearance;
            newPlayer.SpawnAsPlayerObject(clientId);
        }

        /// <summary>
        /// Temp code to spawn an enemy
        /// </summary>
        private void Update()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                var newEnemy = Instantiate(m_EnemyPrefab);
                newEnemy.SpawnWithOwnership(NetworkingManager.Singleton.LocalClientId);
            }
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                var newEnemy = Instantiate(m_BossPrefab);
                newEnemy.SpawnWithOwnership(NetworkingManager.Singleton.LocalClientId);
            }
        }
    }
}
