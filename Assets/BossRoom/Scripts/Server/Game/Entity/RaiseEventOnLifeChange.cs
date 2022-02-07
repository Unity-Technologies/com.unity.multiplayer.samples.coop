using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server-only component which invokes a GameEvent once a LifeState condition is met.
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState))]
    public class RaiseEventOnLifeChange : NetworkBehaviour
    {
        [SerializeField]
        GameEvent m_Event;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        LifeState m_RaiseCondition;

        void Awake()
        {
            // Disable this NetworkBehavior until it is spawned. This prevents unwanted behavior when this is loaded before being spawned, such as during client synchronization
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                enabled = true;

                Assert.IsNotNull(m_NetworkLifeState, "NetworkLifeState has not been set!");

                m_NetworkLifeState.LifeState.OnValueChanged += LifeStateChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                enabled = false;
                m_NetworkLifeState.LifeState.OnValueChanged -= LifeStateChanged;
            }
        }

        void LifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue == m_RaiseCondition)
            {
                m_Event.Raise();
            }
        }
    }
}
