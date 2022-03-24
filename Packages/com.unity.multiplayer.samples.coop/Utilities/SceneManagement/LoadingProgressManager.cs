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

        Dictionary<ulong, NetworkedLoadingProgressTracker> m_ProgressTrackers = new Dictionary<ulong, NetworkedLoadingProgressTracker>();

        public Dictionary<ulong, NetworkedLoadingProgressTracker> ProgressTrackers => m_ProgressTrackers;

        public AsyncOperation LocalLoadOperation;

        float m_LocalProgress;

        public float LocalProgress
        {
            get => IsSpawned && m_ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) ?
                m_ProgressTrackers[NetworkManager.LocalClientId].Progress : m_LocalProgress;
            private set
            {
                if (IsSpawned && m_ProgressTrackers.ContainsKey(NetworkManager.LocalClientId))
                {
                    m_ProgressTrackers[NetworkManager.LocalClientId].Progress = value;
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
        }

        void Update()
        {
            if (LocalLoadOperation != null)
            {
                LocalProgress = LocalLoadOperation.isDone ? 1 : LocalLoadOperation.progress;
            }
            else
            {
                LocalProgress = 0;
            }
        }

        [ClientRpc]
        void UpdateTrackersClientRpc()
        {
            m_ProgressTrackers.Clear();
            foreach (var tracker in FindObjectsOfType<NetworkedLoadingProgressTracker>())
            {
                m_ProgressTrackers[tracker.OwnerClientId] = tracker;
            }
        }

        void AddTracker(ulong clientId)
        {
            var tracker = Instantiate(m_ProgressTrackerPrefab);
            var networkObject = tracker.GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership(clientId);
            UpdateTrackersClientRpc();
        }

        void RemoveTracker(ulong clientId)
        {
            var tracker = m_ProgressTrackers[clientId];
            m_ProgressTrackers.Remove(clientId);
            tracker.NetworkObject.Despawn();
            UpdateTrackersClientRpc();
        }
    }
}
