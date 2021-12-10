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
    /// delay to prevent it from repeatedly loading and unloading the same scene.
    /// </summary>
    public class ServerAdditiveSceneLoader : NetworkBehaviour
    {
        [SerializeField]
        float delayBeforeUnload = 5.0f;

        [SerializeField]
        string sceneName;

        /// <summary>
        /// We assume that all NetworkObjects with this tag are player-owned
        /// </summary>
        [SerializeField]
        string playerTag;

        /// <summary>
        /// We keep the clientIds of every player-owned object inside the collider's volumed
        /// </summary>
        List<ulong> m_PlayersInTrigger;

        bool m_IsLoaded;

        Coroutine m_UnloadCoroutine;

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

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag) && other.TryGetComponent(out NetworkObject networkObject))
            {
                m_PlayersInTrigger.Add(networkObject.OwnerClientId);

                if (m_UnloadCoroutine != null)
                {
                    // stopping the unloading coroutine since there is now a player-owned NetworkObject inside
                    StopCoroutine(m_UnloadCoroutine);
                }

                if (!m_IsLoaded && m_PlayersInTrigger.Count > 0)
                {
                    NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                    m_IsLoaded = true;
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag) && other.TryGetComponent(out NetworkObject networkObject))
            {
                m_PlayersInTrigger.Remove(networkObject.OwnerClientId);
                if (m_IsLoaded && m_PlayersInTrigger.Count == 0)
                {
                    // using a coroutine here to add a delay before unloading the scene
                    m_UnloadCoroutine = StartCoroutine(UnloadCoroutine());
                }
            }
        }

        void RemovePlayer(ulong clientId)
        {
            // remove all references to this clientId. There could be multiple references if a single client owns
            // multiple NetworkObjects with the playerTag, or if this script's GameObject has overlapping colliders
            while (m_PlayersInTrigger.Remove(clientId)) { }
        }

        IEnumerator UnloadCoroutine()
        {
            yield return new WaitForSeconds(delayBeforeUnload);
            if (m_IsLoaded)
            {
                NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneByName(sceneName));
                m_IsLoaded = false;
            }
        }
    }
}
