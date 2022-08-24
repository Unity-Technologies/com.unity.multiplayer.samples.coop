using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// Custom spawning component to be added to a scene GameObject. This component collects NetworkObjects in a scene
    /// marked by a special tag, collects their Transform data, destroys their prefab instance, and performs the dynamic
    /// spawning of said objects during Netcode for GameObject's (Netcode) OnNetworkSpawn() callback.
    /// </summary>
    public class NetworkObjectSpawner : NetworkBehaviour
    {
        [SerializeField]
        List<SpawnObjectData> m_SpawnObjectData;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            StartCoroutine(WaitToSpawnNetworkObjects());
        }

        IEnumerator WaitToSpawnNetworkObjects()
        {
            // must wait for Netcode's OnNetworkSpawn() sweep before dynamically spawning
            yield return new WaitForEndOfFrame();
            SpawnNetworkObjects();
        }

        void SpawnNetworkObjects()
        {
            for (int i = m_SpawnObjectData.Count - 1; i >= 0; i--)
            {
                var spawnedGameObject = Instantiate(m_SpawnObjectData[i].prefabReference,
                    m_SpawnObjectData[i].transform.position,
                    m_SpawnObjectData[i].transform.rotation,
                    null);

                spawnedGameObject.transform.localScale = m_SpawnObjectData[i].transform.lossyScale;
                var spawnedNetworkObject = spawnedGameObject.GetComponent<NetworkObject>();
                spawnedNetworkObject.Spawn();

                Destroy(m_SpawnObjectData[i].gameObject);
            }
        }
    }
}
