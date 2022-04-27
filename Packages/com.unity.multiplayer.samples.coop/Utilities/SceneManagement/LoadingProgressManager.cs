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
        public struct LoadingProgressTracker : IEquatable<LoadingProgressTracker>
        {
            public ulong ClientId;
            public float Progress;
            public bool Equals(LoadingProgressTracker other)
            {
                return ClientId == other.ClientId && Math.Abs(Progress - other.Progress) < k_ProgressDifferenceTolerance;
            }

            const float k_ProgressDifferenceTolerance = 0.1f;
        }

        public NetworkList<LoadingProgressTracker> ProgressTrackers;

        public AsyncOperation LocalLoadOperation
        {
            set
            {
                LocalProgress = 0;
                m_LocalLoadOperation = value;
                StartCoroutine(UpdateLocalProgress());
                if (IsServer)
                {
                    ReinitializeProgressTrackers();
                }
            }
        }

        AsyncOperation m_LocalLoadOperation;

        float m_LocalProgress;

        public float LocalProgress
        {
            get => m_LocalProgress;
            private set
            {
                if (IsSpawned)
                {
                    UpdateClientProgressServerRpc(NetworkManager.LocalClientId, value);
                }
                m_LocalProgress = value;
            }
        }

        void Awake()
        {
            ProgressTrackers = new NetworkList<LoadingProgressTracker>();
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

        IEnumerator UpdateLocalProgress()
        {
            if (m_LocalLoadOperation != null)
            {
                while (!m_LocalLoadOperation.isDone)
                {
                    LocalProgress = m_LocalLoadOperation.progress;
                    yield return new WaitForSeconds(0.2f);
                }

                LocalProgress = 1;
            }
        }

        void ReinitializeProgressTrackers()
        {
            for (var i = 0; i < ProgressTrackers.Count; i++)
            {
                var loadingProgressTracker = ProgressTrackers[i];
                loadingProgressTracker.Progress = 0;
                ProgressTrackers[i] = loadingProgressTracker;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void UpdateClientProgressServerRpc(ulong clientId, float progress)
        {
            for (var i = 0; i < ProgressTrackers.Count; i++)
            {
                var loadingProgressTracker = ProgressTrackers[i];
                if (loadingProgressTracker.ClientId == clientId)
                {
                    loadingProgressTracker.Progress = progress;
                }

                ProgressTrackers[i] = loadingProgressTracker;
            }
        }

        void AddTracker(ulong clientId)
        {
            if (IsServer)
            {
                ProgressTrackers.Add(new LoadingProgressTracker() { ClientId = clientId, Progress = 0 });
            }
        }

        void RemoveTracker(ulong clientId)
        {
            if (IsServer)
            {
                for (var i = 0; i < ProgressTrackers.Count; i++)
                {
                    var loadingProgressTracker = ProgressTrackers[i];
                    if (loadingProgressTracker.ClientId == clientId)
                    {
                        ProgressTrackers.RemoveAt(i);
                    }
                }
            }
        }
    }
}
