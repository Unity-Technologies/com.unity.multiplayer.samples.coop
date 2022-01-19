using System;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Debug
{
    /// <summary>
    /// Handles debug cheat events, applies them on the server and logs them on all clients. This class is only
    /// available in the editor or for development builds
    /// </summary>
    public class DebugCheatsManager : NetworkBehaviour
    {
        [SerializeField]
        GameObject m_DebugCheatsPanel;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField]
        [Tooltip("Enemy to spawn. Make sure this is included in the NetworkManager's list of prefabs!")]
        NetworkObject m_EnemyPrefab;

        [SerializeField]
        [Tooltip("Boss to spawn. Make sure this is included in the NetworkManager's list of prefabs!")]
        NetworkObject m_BossPrefab;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode = KeyCode.Slash;

        const int k_NbTouchesToOpenWindow = 4;

        void Update()
        {
            if (Input.touchCount == k_NbTouchesToOpenWindow ||
                m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
            {
                m_DebugCheatsPanel.SetActive(!m_DebugCheatsPanel.activeSelf);
            }
        }

        public void SpawnEnemy()
        {
            SpawnEnemyServerRpc();
        }

        public void SpawnBoss()
        {
            SpawnBossServerRpc();
        }

        public void ToggleTeleportMode()
        {
            ToggleTeleportModeServerRpc();
        }

        public void GoToPostGame()
        {
            GoToPostGameServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void SpawnEnemyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var newEnemy = Instantiate(m_EnemyPrefab);
            newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "SpawnEnemy");
        }

        [ServerRpc(RequireOwnership = false)]
        void SpawnBossServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var newEnemy = Instantiate(m_BossPrefab);
            newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "SpawnBoss");
        }

        [ServerRpc(RequireOwnership = false)]
        void ToggleTeleportModeServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            foreach (var playerServerCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (playerServerCharacter.OwnerClientId == clientId)
                {
                    playerServerCharacter.Movement.TeleportModeActivated = !playerServerCharacter.Movement.TeleportModeActivated;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void GoToPostGameServerRpc(ServerRpcParams serverRpcParams = default)
        {
            NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
            LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "GoToPostGame");
        }

        [ClientRpc]
        void LogCheatUsedClientRPC(ulong clientId, string cheatUsed)
        {
            UnityEngine.Debug.Log($"Cheat {cheatUsed} used by client {clientId}");
        }

#else
        void Awake()
        {
            m_DebugCheatsPanel.SetActive(false);
        }
#endif
    }
}
