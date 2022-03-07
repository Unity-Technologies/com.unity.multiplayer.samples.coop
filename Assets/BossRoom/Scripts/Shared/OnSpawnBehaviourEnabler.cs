using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// Classes implementing this interface get disabled on Awake by the OnSpawnBehaviorEnabler, then only enabled when
    /// it is spawned on a client.
    ///<remarks>
    /// Any class implementing this should add the "[RequireComponent(typeof(OnSpawnBehaviourEnabler))]" attribute
    /// </remarks>
    /// </summary>
    public interface IClientOnlyMonoBehaviour
    {
        public void SetEnabled(bool enable);
    }

    /// <summary>
    /// Classes implementing this interface get disabled on Awake by the OnSpawnBehaviorEnabler, then only enabled when
    /// it is spawned on a server.
    ///<remarks>
    /// Any class implementing this should add the "[RequireComponent(typeof(OnSpawnBehaviourEnabler))]" attribute
    /// </remarks>
    /// </summary>
    public interface IServerOnlyMonoBehaviour
    {
        public void SetEnabled(bool enable);
    }


    /// <summary>
    /// Disables a list of MonoBehaviours on Awake, then only enables them when spawning based on whether this game instance
    /// is a client or a server (or both in case of a client-hosted session).
    /// </summary>
    public class OnSpawnBehaviourEnabler : NetworkBehaviour
    {
        List<IClientOnlyMonoBehaviour> m_ClientOnlyMonoBehaviours;
        List<IServerOnlyMonoBehaviour> m_ServerOnlyMonoBehaviours;

        void Awake()
        {
            m_ClientOnlyMonoBehaviours = new List<IClientOnlyMonoBehaviour>();
            m_ServerOnlyMonoBehaviours = new List<IServerOnlyMonoBehaviour>();
            foreach (var behaviour in gameObject.GetComponents<IClientOnlyMonoBehaviour>())
            {
                m_ClientOnlyMonoBehaviours.Add(behaviour);
            }
            foreach (var behaviour in gameObject.GetComponents<IServerOnlyMonoBehaviour>())
            {
                m_ServerOnlyMonoBehaviours.Add(behaviour);
            }
            // Disable everything here to prevent those MonoBehaviours to be updated before this one is spawned.
            DisableAll();
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                foreach (var behaviour in m_ClientOnlyMonoBehaviours)
                {
                    behaviour.SetEnabled(true);
                }
            }

            if (IsServer)
            {
                foreach (var behaviour in m_ServerOnlyMonoBehaviours)
                {
                    behaviour.SetEnabled(true);
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
            foreach (var behaviour in m_ClientOnlyMonoBehaviours)
            {
                behaviour.SetEnabled(false);
            }

            foreach (var behaviour in m_ServerOnlyMonoBehaviours)
            {
                behaviour.SetEnabled(false);
            }
        }
    }
}
