using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// A utility class that can be added to a NetworkObject to collect its NetworkBehaviours for quick lookups.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkBehaviourLookup : MonoBehaviour
    {
        [SerializeField]
        NetworkBehaviour[] m_NetworkBehaviours;

        public bool TryGetNetworkBehaviour<T>(out T networkBehaviour) where T : NetworkBehaviour
        {
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                if (m_NetworkBehaviours[i].GetType() == typeof(T))
                {
                    networkBehaviour = (T)m_NetworkBehaviours[i];
                    return true;
                }
            }

            networkBehaviour = null;
            return false;
        }
    }
}
