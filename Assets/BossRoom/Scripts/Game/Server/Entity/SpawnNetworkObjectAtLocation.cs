using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Game
{
    public class SpawnNetworkObjectAtLocation : NetworkBehaviour
    {
        [SerializeField]
        GameObject m_NetworkObjectPrefab;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            NetworkManager.SceneManager.OnLoadEventCompleted += SpawnNetworkPrefab;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnNetworkPrefab;
        }

        void SpawnNetworkPrefab(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            // spawn NetworkObject prefab at this transform's position & with this transform's rotation
            var clone = Instantiate(m_NetworkObjectPrefab, transform.position, transform.rotation);
            var cloneNetworkObject = clone.GetComponent<NetworkObject>();
            cloneNetworkObject.Spawn();
        }
    }
}
