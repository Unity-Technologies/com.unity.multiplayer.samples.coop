using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.DebugCheats
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

        SwitchedDoor m_SwitchedDoor;

        SwitchedDoor SwitchedDoor
        {
            get
            {
                if (m_SwitchedDoor == null)
                {
                    m_SwitchedDoor = FindObjectOfType<SwitchedDoor>();
                }
                return m_SwitchedDoor;
            }
        }

        const int k_NbTouchesToOpenWindow = 4;

        bool m_DestroyPortalsOnNextToggle = true;

        [Inject]
        IPublisher<CheatUsedMessage> m_CheatUsedMessagePublisher;

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
            ServerSpawnEnemyRpc();
        }

        public void SpawnBoss()
        {
            ServerSpawnBossRpc();
        }

        public void KillTarget()
        {
            ServerKillTargetRpc();
        }

        public void KillAllEnemies()
        {
            ServerKillAllEnemiesRpc();
        }

        public void ToggleGodMode()
        {
            ServerToggleGodModeRpc();
        }

        public void HealPlayer()
        {
            ServerHealPlayerRpc();
        }

        public void ToggleSuperSpeed()
        {
            ServerToggleSuperSpeedRpc();
        }

        public void ToggleTeleportMode()
        {
            ServerToggleTeleportModeRpc();
        }

        public void ToggleDoor()
        {
            ServerToggleDoorRpc();
        }

        public void TogglePortals()
        {
            ServerTogglePortalsRpc();
        }

        public void GoToPostGame()
        {
            GoToPostGameServerRpc();
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerSpawnEnemyRpc(RpcParams serverRpcParams = default)
        {
            var newEnemy = Instantiate(m_EnemyPrefab);
            newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "SpawnEnemy");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerSpawnBossRpc(RpcParams serverRpcParams = default)
        {
            var newEnemy = Instantiate(m_BossPrefab);
            newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "SpawnBoss");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerKillTargetRpc(RpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                var targetId = playerServerCharacter.TargetId.Value;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject obj))
                {
                    var damageable = obj.GetComponent<IDamageable>();
                    if (damageable != null && damageable.IsDamageable())
                    {
                        damageable.ReceiveHP(playerServerCharacter, int.MinValue);
                        PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "KillTarget");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Target {targetId} has no IDamageable component or cannot be damaged.");
                    }
                }

            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerKillAllEnemiesRpc(RpcParams serverRpcParams = default)
        {
            foreach (var serverCharacter in FindObjectsOfType<ServerCharacter>())
            {
                if (serverCharacter.IsNpc && serverCharacter.LifeState == LifeState.Alive)
                {
                    if (serverCharacter.gameObject.TryGetComponent(out IDamageable damageable))
                    {
                        damageable.ReceiveHP(null, -serverCharacter.HitPoints);
                    }
                }
            }
            PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "KillAllEnemies");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerToggleGodModeRpc(RpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                playerServerCharacter.NetLifeState.IsGodMode.Value = !playerServerCharacter.NetLifeState.IsGodMode.Value;
                PublishCheatUsedMessage(clientId, "ToggleGodMode");
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerHealPlayerRpc(RpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            var playerServerCharacter = PlayerServerCharacter.GetPlayerServerCharacter(clientId);
            if (playerServerCharacter != null)
            {
                var baseHp = playerServerCharacter.CharacterClass.BaseHP.Value;
                if (playerServerCharacter.LifeState == LifeState.Fainted)
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
                PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "HealPlayer");
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerToggleSuperSpeedRpc(RpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            foreach (var playerServerCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (playerServerCharacter.OwnerClientId == clientId)
                {
                    playerServerCharacter.Movement.SpeedCheatActivated = !playerServerCharacter.Movement.SpeedCheatActivated;
                    break;
                }
            }
            PublishCheatUsedMessage(clientId, "ToggleSuperSpeed");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerToggleTeleportModeRpc(RpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            foreach (var playerServerCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (playerServerCharacter.OwnerClientId == clientId)
                {
                    playerServerCharacter.Movement.TeleportModeActivated = !playerServerCharacter.Movement.TeleportModeActivated;
                    break;
                }
            }
            PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "ToggleTeleportMode");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerToggleDoorRpc(RpcParams serverRpcParams = default)
        {
            if (SwitchedDoor != null)
            {
                SwitchedDoor.ForceOpen = !SwitchedDoor.ForceOpen;
                PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "ToggleDoor");
            }
            else
            {
                UnityEngine.Debug.Log("Could not activate ToggleDoor cheat. Door not found.");
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ServerTogglePortalsRpc(RpcParams serverRpcParams = default)
        {
            foreach (var portal in FindObjectsOfType<EnemyPortal>())
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
            PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "TogglePortals");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void GoToPostGameServerRpc(RpcParams serverRpcParams = default)
        {
            SceneLoaderWrapper.Instance.LoadScene("PostGame", useNetworkSceneManager: true);
            PublishCheatUsedMessage(serverRpcParams.Receive.SenderClientId, "GoToPostGame");
        }

        void PublishCheatUsedMessage(ulong clientId, string cheatUsed)
        {
            var playerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
            if (playerData.HasValue)
            {
                m_CheatUsedMessagePublisher.Publish(new CheatUsedMessage(cheatUsed, playerData.Value.PlayerName));
            }
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
