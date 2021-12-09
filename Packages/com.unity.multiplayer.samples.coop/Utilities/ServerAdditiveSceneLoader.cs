using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// This NetworkBehavior, when added to a GameObject containing a collider (or multiple colliders) with the
    /// IsTrigger property On, allows the server to load or unload a scene additively according to the position of
    /// player-owned objects. The scene is loaded when there is at least one NetworkObject with the specified tag that
    /// enters its collider. It also unloads it when all such NetworkObjects leave the collider, after a specified
    /// cooldown to prevent it from repeatedly loading and unloading the same scene.
    /// </summary>
    public class ServerAdditiveSceneLoader : NetworkBehaviour
    {
        [SerializeField]
        float cooldownBeforeUnload = 5.0f;

        [SerializeField]
        string sceneName;

        [SerializeField]
        string playerTag;

        List<ulong> m_PlayersInTrigger;

        bool m_IsLoaded;

        bool m_IsCooldown;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Adding this to remove all pending references to a specific client when they disconnect, since objects
                // that are destroyed do not generate OnTriggerExit events.
                NetworkManager.OnClientDisconnectCallback += RemovePlayer;
                m_PlayersInTrigger = new List<ulong>();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
            }
        }

        void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (!m_IsCooldown)
            {
                if (m_IsLoaded && m_PlayersInTrigger.Count == 0)
                {
                    NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneByName(sceneName));
                    m_IsLoaded = false;
                }
                else if (!m_IsLoaded && m_PlayersInTrigger.Count > 0)
                {
                    NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    m_IsLoaded = true;

                    // Add this delay to prevent players entering and leaving the collider repeatedly from continually load/unloading the scene
                    StartCoroutine(Cooldown());
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag) && other.TryGetComponent(out NetworkObject networkObject))
            {
                m_PlayersInTrigger.Add(networkObject.OwnerClientId);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag) && other.TryGetComponent(out NetworkObject networkObject))
            {
                m_PlayersInTrigger.Remove(networkObject.OwnerClientId);
            }
        }

        void RemovePlayer(ulong clientId)
        {
            // remove all references to this clientId
            while (m_PlayersInTrigger.Remove(clientId)) { }
        }

        IEnumerator Cooldown()
        {
            m_IsCooldown = true;
            yield return new WaitForSeconds(cooldownBeforeUnload);
            m_IsCooldown = false;
        }
    }
}
