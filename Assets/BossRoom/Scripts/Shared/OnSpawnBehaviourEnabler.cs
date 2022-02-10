using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    public interface IClientOnlyMonoBehaviour
    {
        public void SetEnabled(bool enable);
    }

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
        void Awake()
        {
            // Disable everything here to prevent those MonoBehaviours to be updated before this one is spawned.
            DisableAll();
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                foreach (var behaviour in gameObject.GetComponents<IClientOnlyMonoBehaviour>())
                {
                    behaviour.SetEnabled(true);
                }
            }

            if (IsServer)
            {
                foreach (var behaviour in gameObject.GetComponents<IServerOnlyMonoBehaviour>())
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
            foreach (var behaviour in gameObject.GetComponents<IClientOnlyMonoBehaviour>())
            {
                behaviour.SetEnabled(false);
            }

            foreach (var behaviour in gameObject.GetComponents<IServerOnlyMonoBehaviour>())
            {
                behaviour.SetEnabled(false);
            }
        }
    }
}
