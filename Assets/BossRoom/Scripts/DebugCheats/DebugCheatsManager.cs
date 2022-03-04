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

        bool m_DestroyPortalsOnNextToggle = true;

        void Update()
        {
            if (Input.touchCount == k_NbTouchesToOpenWindow && AnyTouchDown() ||
                m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
            {
                m_DebugCheatsPanel.SetActive(!m_DebugCheatsPanel.activeSelf);
            }
        }

        static bool AnyTouchDown()
        {
            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }
            return false;
        }

        public void SpawnEnemy()
        {
            SpawnEnemyServerRpc();
        }

        public void SpawnBoss()
        {
            SpawnBossServerRpc();
        }

        public void KillTarget()
        {
            KillTargetServerRpc();
        }

        public void KillAllEnemies()
        {
            KillAllEnemiesServerRpc();
        }

        public void ToggleGodMode()
        {
            ToggleGodModeServerRpc();
        }

        public void HealPlayer()
        {
            HealPlayerServerRpc();
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
            TogglePortalsServerRpc();
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
        void KillTargetServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                var targetId = playerServerCharacter.NetState.TargetId.Value;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject obj))
                {
                    var damageable = obj.GetComponent<IDamageable>();
                    if (damageable != null && damageable.IsDamageable())
                    {
                        damageable.ReceiveHP(playerServerCharacter, int.MinValue);
                        LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "KillTarget");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Target {targetId} has no IDamageable component or cannot be damaged.");
                    }
                }

            }
        }

        [ServerRpc(RequireOwnership = false)]
        void KillAllEnemiesServerRpc(ServerRpcParams serverRpcParams = default)
        {
            foreach (var serverCharacter in FindObjectsOfType<ServerCharacter>())
            {
                if (serverCharacter.IsNpc && serverCharacter.NetState.LifeState == LifeState.Alive)
                {
                    if (serverCharacter.gameObject.TryGetComponent(out IDamageable damageable))
                    {
                        damageable.ReceiveHP(null, -serverCharacter.NetState.HitPoints);
                    }
                }
            }
            LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "KillAllEnemies");
        }

        [ServerRpc(RequireOwnership = false)]
        void ToggleGodModeServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                playerServerCharacter.NetState.NetworkLifeState.IsGodMode.Value = !playerServerCharacter.NetState.NetworkLifeState.IsGodMode.Value;
                LogCheatUsedClientRPC(clientId, "ToggleGodMode");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void HealPlayerServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
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
                    if (playerServerCharacter.gameObject.TryGetComponent(out IDamageable damageable))
                    {
                        damageable.ReceiveHP(null, baseHp);
                    }
                }
                LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "HealPlayer");
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void TogglePortalsServerRpc(ServerRpcParams serverRpcParams = default)
        {
            foreach (var portal in FindObjectsOfType<ServerEnemyPortal>())
            {
                if (m_DestroyPortalsOnNextToggle)
                {
                    // This will only affect portals that are currently active in a scene and are currently loaded.
                    // Portals that are already destroyed will not be affected by this, and won't have their cooldown
                    // reinitialized.
                    portal.ForceDestroy();
                }
                else
                {
                    portal.ForceRestart();
                }
            }

            m_DestroyPortalsOnNextToggle = !m_DestroyPortalsOnNextToggle;
            LogCheatUsedClientRPC(serverRpcParams.Receive.SenderClientId, "TogglePortals");
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

        static void LogCheatNotImplemented(string cheat)
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
