using MLAPI;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Server
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

        public override void NetworkStart()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            Assert.IsNotNull(m_NetworkLifeState, "NetworkLifeState has not been set!");

            m_NetworkLifeState.LifeState.OnValueChanged += LifeStateChanged;
        }

        void OnDestroy()
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
