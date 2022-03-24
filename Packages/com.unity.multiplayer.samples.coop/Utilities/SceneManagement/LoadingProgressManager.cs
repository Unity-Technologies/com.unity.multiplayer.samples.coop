using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
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
            get => m_LocalProgress;
            private set
            {
                if (IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId))
                {
                    ProgressTrackers[NetworkManager.LocalClientId].Progress.Value = value;
                    m_LocalProgress = value;
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
            ProgressTrackers.Clear();
            foreach (var tracker in FindObjectsOfType<NetworkedLoadingProgressTracker>())
            {
                ProgressTrackers[tracker.OwnerClientId] = tracker;
                if (tracker.OwnerClientId == NetworkManager.LocalClientId)
                {
                    LocalProgress = Mathf.Max(m_LocalProgress, LocalProgress);
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
                UpdateTrackersClientRpc();
            }
        }
    }
}
