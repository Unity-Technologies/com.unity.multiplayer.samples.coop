using Unity.Netcode;
using UnityEngine;

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

            NetworkManager.SceneManager.OnSceneEvent += SpawnNetworkPrefab;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnSceneEvent -= SpawnNetworkPrefab;
        }

        void SpawnNetworkPrefab(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                // spawn NetworkObject prefab at this transform's position & with this transform's rotation
                var clone = Instantiate(m_NetworkObjectPrefab, transform.position, transform.rotation);
                var cloneNetworkObject = clone.GetComponent<NetworkObject>();
                cloneNetworkObject.Spawn();
            }
        }
    }
}
