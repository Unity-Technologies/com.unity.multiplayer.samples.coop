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
        float m_DelayBeforeUnload = 5.0f;

        [SerializeField]
        string m_SceneName;

        /// <summary>
        /// We assume that all NetworkObjects with this tag are player-owned
        /// </summary>
        [SerializeField]
        string m_PlayerTag;

        /// <summary>
        /// We keep the clientIds of every player-owned object inside the collider's volume
        /// </summary>
        List<ulong> m_PlayersInTrigger;

        bool IsActive => IsServer && IsSpawned;

        enum SceneState
        {
            Loaded,
            Unloaded,
            Loading,
            Unloading,
            WaitingToUnload,
        }

        SceneState m_SceneState = SceneState.Unloaded;

        Coroutine m_UnloadCoroutine;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Adding this to remove all pending references to a specific client when they disconnect, since objects
                // that are destroyed do not generate OnTriggerExit events.
                NetworkManager.OnClientDisconnectCallback += RemovePlayer;

                NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                m_PlayersInTrigger = new List<ulong>();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
                NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && sceneEvent.SceneName == m_SceneName)
            {
                m_SceneState = SceneState.Loaded;
            }
            else if (sceneEvent.SceneEventType == SceneEventType.UnloadEventCompleted && sceneEvent.SceneName == m_SceneName)
            {
                m_SceneState = SceneState.Unloaded;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsActive) // make sure that OnNetworkSpawn has been called before this
            {
                if (other.CompareTag(m_PlayerTag) && other.TryGetComponent(out NetworkObject networkObject))
                {
                    m_PlayersInTrigger.Add(networkObject.OwnerClientId);

                    if (m_UnloadCoroutine != null)
                    {
                        // stopping the unloading coroutine since there is now a player-owned NetworkObject inside
                        StopCoroutine(m_UnloadCoroutine);
                        if (m_SceneState == SceneState.WaitingToUnload)
                        {
                            m_SceneState = SceneState.Loaded;
                        }
                    }
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (IsActive) // make sure that OnNetworkSpawn has been called before this
            {
                if (other.CompareTag(m_PlayerTag) && other.TryGetComponent(out NetworkObject networkObject))
                {
                    m_PlayersInTrigger.Remove(networkObject.OwnerClientId);
                }
            }
        }

        void FixedUpdate()
        {
            if (IsActive) // make sure that OnNetworkSpawn has been called before this
            {
                if (m_SceneState == SceneState.Unloaded && m_PlayersInTrigger.Count > 0)
                {
                    var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
                    // if successfully started a LoadScene event, set state to Loading
                    if (status == SceneEventProgressStatus.Started)
                    {
                        m_SceneState = SceneState.Loading;
                    }
                }
                else if (m_SceneState == SceneState.Loaded && m_PlayersInTrigger.Count == 0)
                {
                    // using a coroutine here to add a delay before unloading the scene
                    m_UnloadCoroutine = StartCoroutine(WaitToUnloadCoroutine());
                    m_SceneState = SceneState.WaitingToUnload;
                }
            }
        }

        void RemovePlayer(ulong clientId)
        {
            // remove all references to this clientId. There could be multiple references if a single client owns
            // multiple NetworkObjects with the m_PlayerTag, or if this script's GameObject has overlapping colliders
            while (m_PlayersInTrigger.Remove(clientId)) { }
        }

        IEnumerator WaitToUnloadCoroutine()
        {
            yield return new WaitForSeconds(m_DelayBeforeUnload);
            Scene scene = SceneManager.GetSceneByName(m_SceneName);
            if (scene.isLoaded)
            {
                var status = NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneByName(m_SceneName));
                // if successfully started an UnloadScene event, set state to Unloading, if not, reset state to Loaded so a new Coroutine will start
                m_SceneState = status == SceneEventProgressStatus.Started ? SceneState.Unloading : SceneState.Loaded;
            }
        }
    }
}
