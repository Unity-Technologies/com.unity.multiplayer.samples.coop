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
    /// IsTrigger property On, allows the server to load a scene additively when there is at least one GameObject with
    /// the specified tag that enters its collider. It also unloads it when all players leave the collider, after a
    /// specified cooldown to prevent it from repeatedly loading and unloading the same scene.
    /// </summary>
    public class ServerAdditiveSceneLoader : NetworkBehaviour
    {
        [SerializeField]
        float cooldownBeforeUnload = 5.0f;

        [SerializeField]
        string sceneName;

        [SerializeField]
        string triggeringTag;

        List<ulong> m_PlayersInTrigger;

        bool m_IsLoaded;

        bool m_IsCooldown;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += RemovePlayer;
                m_PlayersInTrigger = new List<ulong>();
            }
            else
            {
                enabled = false;
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
            if (other.CompareTag(triggeringTag) && other.TryGetComponent(out NetworkObject networkObject))
            {
                m_PlayersInTrigger.Add(networkObject.OwnerClientId);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(triggeringTag) && other.TryGetComponent(out NetworkObject networkObject))
            {
                RemovePlayer(networkObject.OwnerClientId);
            }
        }

        void RemovePlayer(ulong clientId)
        {
            m_PlayersInTrigger.Remove(clientId);
        }

        IEnumerator Cooldown()
        {
            m_IsCooldown = true;
            yield return new WaitForSeconds(cooldownBeforeUnload);
            m_IsCooldown = false;
        }
    }
}
