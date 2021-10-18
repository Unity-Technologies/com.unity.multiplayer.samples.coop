using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Provides various special commands that the Host can use while developing and
    /// debugging the game. To disable them, just disable or remove this component from the
    /// BossRoomState prefab.
    /// </summary>
    public class ServerTestingHotkeys : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Enemy to spawn. Make sure this is included in the NetworkManager's list of prefabs!")]
        private NetworkObject m_EnemyPrefab;

        [SerializeField]
        [Tooltip("Boss to spawn. Make sure this is included in the NetworkManager's list of prefabs!")]
        private NetworkObject m_BossPrefab;

        [SerializeField]
        [Tooltip("Key that the Host can press to spawn an extra enemy")]
        KeyCode m_SpawnEnemyKeyCode = KeyCode.E;

        [SerializeField]
        [Tooltip("Key that the Host can press to spawn an extra boss")]
        KeyCode m_SpawnBossKeyCode = KeyCode.B;

        [SerializeField]
        [Tooltip("Key that the Host can press to quit the game")]
        KeyCode m_InstantQuitKeyCode = KeyCode.Q;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                // these commands don't work on the client
                enabled = false;
            }
        }

        private void Update()
        {
            if (!IsServer) { return; } // not initialized yet

            if (m_SpawnEnemyKeyCode != KeyCode.None && Input.GetKeyDown(m_SpawnEnemyKeyCode))
            {
                var newEnemy = Instantiate(m_EnemyPrefab);
                newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            }
            if (m_SpawnBossKeyCode != KeyCode.None && Input.GetKeyDown(m_SpawnBossKeyCode))
            {
                var newEnemy = Instantiate(m_BossPrefab);
                newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            }
            if (m_InstantQuitKeyCode != KeyCode.None && Input.GetKeyDown(m_InstantQuitKeyCode))
            {
                NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
            }
        }
    }
}
