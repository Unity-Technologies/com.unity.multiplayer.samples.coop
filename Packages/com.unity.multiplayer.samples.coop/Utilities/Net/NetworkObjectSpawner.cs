using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class NetworkObjectSpawner : MonoBehaviour
    {
        public NetworkObject prefabReference;

        public void Awake()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsServer &&
                NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManagerOnOnLoadEventCompleted;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsServer &&
                NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManagerOnOnLoadEventCompleted;
            }
        }

        void SceneManagerOnOnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            SpawnNetworkObject();
            Destroy(gameObject);
        }

        void SpawnNetworkObject()
        {
            var instantiatedNetworkObject = Instantiate(prefabReference, transform.position, transform.rotation, null);
            instantiatedNetworkObject.transform.localScale = transform.lossyScale;
            instantiatedNetworkObject.Spawn();
        }
    }
}
