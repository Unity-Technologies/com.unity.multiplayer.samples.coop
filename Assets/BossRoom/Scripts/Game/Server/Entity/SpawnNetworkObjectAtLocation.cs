using System.Collections;
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

        void SpawnNetworkPrefab(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                StartCoroutine(Spawn());
            }
        }

        IEnumerator Spawn()
        {
            yield return new WaitForSeconds(1f);
            var clone = Instantiate(m_NetworkObjectPrefab, transform.position, transform.rotation);
            var cloneNetworkObject = clone.GetComponent<NetworkObject>();
            cloneNetworkObject.Spawn();
        }
    }
}
