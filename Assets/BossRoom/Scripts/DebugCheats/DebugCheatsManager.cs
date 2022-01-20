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

        public void KillRandomEnemy()
        {
            LogCheatNotImplemented("KillRandomEnemy");
        }

        public void KillAllEnemies()
        {
            LogCheatNotImplemented("KillAllEnemies");
        }

        public void ToggleGodMode()
        {
            ToggleGodModeServerRpc();
        }

        public void HealPlayer()
        {
            HealPlayerServerRpc();
        }

        public void KillPlayer()
        {
            KillPlayerServerRpc();
        }

        public void ToggleSuperSpeed()
        {
            LogCheatNotImplemented("ToggleSuperSpeed");
        }

        public void ToggleTeleportMode()
        {
            LogCheatNotImplemented("ToggleTeleportMode");
        }

        public void ToggleDoor()
        {
            LogCheatNotImplemented("ToggleDoor");
        }

        public void TogglePortals()
        {
            LogCheatNotImplemented("TogglePortals");
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
        void ToggleGodModeServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                playerServerCharacter.NetState.NetworkLifeState.IsGodMode.Value = !playerServerCharacter.NetState.NetworkLifeState.IsGodMode.Value;
                LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "ToggleGodMode");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void HealPlayerServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                var baseHp = playerServerCharacter.NetState.CharacterClass.BaseHP.Value;
                if (playerServerCharacter.NetState.LifeState == LifeState.Fainted)
                {
                    playerServerCharacter.Revive(null, baseHp);
                }
                else
                {
                    playerServerCharacter.ReceiveHP(null, baseHp);
                }
                LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "HealPlayer");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void KillPlayerServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                playerServerCharacter.ReceiveHP(null, -playerServerCharacter.NetState.HitPoints);
                LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "KillPlayer");
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

        void LogCheatNotImplemented(string cheat)
        {
            UnityEngine.Debug.Log($"Cheat {cheat} not implemented");
        }

#else
        void Awake()
        {
            m_DebugCheatsPanel.SetActive(false);
        }
#endif
    }
}
