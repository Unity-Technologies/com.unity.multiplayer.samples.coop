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

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            Assert.IsNotNull(m_NetworkLifeState, "NetworkLifeState has not been set!");

            m_NetworkLifeState.LifeState.OnValueChanged += LifeStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            m_NetworkLifeState.LifeState.OnValueChanged -= LifeStateChanged;
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
