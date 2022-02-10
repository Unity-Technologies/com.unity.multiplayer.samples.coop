using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{

    /// <summary>
    /// Disables a list of MonoBehaviours on Awake, then only enables them when spawning based on whether this game instance
    /// is a client or a server (or both in case of a client-hosted session).
    /// </summary>
    public class OnSpawnBehaviorEnabler : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("These MonoBehaviours will be disabled on Awake and only enabled when this NetworkBehaviour is spawned on a client.")]
        List<MonoBehaviour> m_ClientOnlyBehaviours;

        [SerializeField]
        [Tooltip("These MonoBehaviours will be disabled on Awake and only enabled when this NetworkBehaviour is spawned on a server.")]
        List<MonoBehaviour> m_ServerOnlyBehaviours;

        void Awake()
        {
            // Disable everything here to prevent those MonoBehaviours to be updated before this one is spawned.
            DisableAll();
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                foreach (var behaviour in m_ClientOnlyBehaviours)
                {
                    behaviour.enabled = true;
                }
            }

            if (IsServer)
            {
                foreach (var behaviour in m_ServerOnlyBehaviours)
                {
                    behaviour.enabled = true;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            // Disable everything and wait until it is spawned again to enable them again
            DisableAll();
        }

        void DisableAll()
        {
            foreach (var behaviour in m_ClientOnlyBehaviours)
            {
                behaviour.enabled = false;
            }

            foreach (var behaviour in m_ServerOnlyBehaviours)
            {
                behaviour.enabled = false;
            }
        }
    }
}
