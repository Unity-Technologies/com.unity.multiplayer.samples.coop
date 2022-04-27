using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// Contains data on scene loading progress for the local instance and remote instances.
    /// </summary>
    public class LoadingProgressManager : NetworkBehaviour
    {
        [SerializeField]
        GameObject m_ProgressTrackerPrefab;

        public Dictionary<ulong, NetworkedLoadingProgressTracker> ProgressTrackers { get; } = new Dictionary<ulong, NetworkedLoadingProgressTracker>();

        public AsyncOperation LocalLoadOperation
        {
            set
            {
                LocalProgress = 0;
                m_LocalLoadOperation = value;
            }
        }

        AsyncOperation m_LocalLoadOperation;

        float m_LocalProgress;
        public event Action onTrackersUpdated;

        public float LocalProgress
        {
            get => IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) ?
                ProgressTrackers[NetworkManager.LocalClientId].Progress.Value : m_LocalProgress;
            private set
            {
                if (IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId))
                {
                    ProgressTrackers[NetworkManager.LocalClientId].Progress.Value = value;
                }
                else
                {
                    m_LocalProgress = value;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddTracker;
                NetworkManager.OnClientDisconnectCallback += RemoveTracker;
                AddTracker(NetworkManager.LocalClientId);
            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= AddTracker;
                NetworkManager.OnClientDisconnectCallback -= RemoveTracker;
            }
            ProgressTrackers.Clear();
        }

        void Update()
        {
            if (m_LocalLoadOperation != null)
            {
                LocalProgress = m_LocalLoadOperation.isDone ? 1 : m_LocalLoadOperation.progress;
            }
        }

        [ClientRpc]
        void UpdateTrackersClientRpc()
        {
            if (!IsHost)
            {
                ProgressTrackers.Clear();
                foreach (var tracker in FindObjectsOfType<NetworkedLoadingProgressTracker>())
                {
                    ProgressTrackers[tracker.OwnerClientId] = tracker;
                    if (tracker.OwnerClientId == NetworkManager.LocalClientId)
                    {
                        LocalProgress = Mathf.Max(m_LocalProgress, LocalProgress);
                    }
                }
            }
            onTrackersUpdated?.Invoke();
        }

        void AddTracker(ulong clientId)
        {
            if (IsServer)
            {
                var tracker = Instantiate(m_ProgressTrackerPrefab);
                var networkObject = tracker.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(clientId);
                ProgressTrackers[clientId] = tracker.GetComponent<NetworkedLoadingProgressTracker>();
                UpdateTrackersClientRpc();
            }
        }

        void RemoveTracker(ulong clientId)
        {
            if (IsServer)
            {
                var tracker = ProgressTrackers[clientId];
                ProgressTrackers.Remove(clientId);
                tracker.NetworkObject.Despawn();
                // This makes sure that clients received the Despawn message before the RPC.
                StartCoroutine(WaitBeforeSendingRPC());
            }
        }

        IEnumerator WaitBeforeSendingRPC()
        {
            yield return null;
            UpdateTrackersClientRpc();
        }
    }
}
