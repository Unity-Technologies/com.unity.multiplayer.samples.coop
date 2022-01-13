using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Provides various special commands that the Host can use while developing and
    /// debugging the game. To disable them, just disable or remove this component from the
    /// BossRoomState prefab.
    /// </summary>
    public class ServerDebugCheatsManager : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Enemy to spawn. Make sure this is included in the NetworkManager's list of prefabs!")]
        NetworkObject m_EnemyPrefab;

        [SerializeField]
        [Tooltip("Boss to spawn. Make sure this is included in the NetworkManager's list of prefabs!")]
        NetworkObject m_BossPrefab;

        [SerializeField]
        DebugCheatsMediator m_DebugCheatsMediator;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_DebugCheatsMediator.SpawnEnemy += SpawnEnemy;
                m_DebugCheatsMediator.SpawnBoss += SpawnBoss;
                m_DebugCheatsMediator.GoToPostGame += GoToPostGame;
                m_DebugCheatsMediator.ToggleGodMode += ToggleGodMode;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                m_DebugCheatsMediator.SpawnEnemy -= SpawnEnemy;
                m_DebugCheatsMediator.SpawnBoss -= SpawnBoss;
                m_DebugCheatsMediator.GoToPostGame -= GoToPostGame;
                m_DebugCheatsMediator.ToggleGodMode -= ToggleGodMode;
            }
        }

        void SpawnEnemy(ulong clientId)
        {
            var newEnemy = Instantiate(m_EnemyPrefab);
            newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
        }

        void SpawnBoss(ulong clientId)
        {
            var newEnemy = Instantiate(m_BossPrefab);
            newEnemy.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
        }

        void GoToPostGame(ulong clientId)
        {
            NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
        }

        void ToggleGodMode(ulong clientId)
        {
            foreach (var playerServerCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (playerServerCharacter.OwnerClientId == clientId)
                {
                    playerServerCharacter.NetState.NetworkLifeState.IsGodMode.Value = !playerServerCharacter.NetState.NetworkLifeState.IsGodMode.Value;
                }
            }
        }
    }
}
#endif
