using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// MonoBehaviour containing only one NetworkVariableInt which represents this object's health.
    /// </summary>
    public class NetworkHealthState : NetworkBehaviour, INetworkSubscribable<int>
    {
        [SerializeField]
        NetworkVariableInt m_NetworkHealth = new NetworkVariableInt();

        public int NetworkHealth
        {
            get => m_NetworkHealth.Value;
            set => m_NetworkHealth.Value = value;
        }

        // public subscribable event to be invoked when HP has been fully depleted
        public event Action HitPointsDepleted;

        // public subscribable event to be invoked when HP has been replenished
        public event Action HitPointsReplenished;

        public void AddListener(NetworkVariable<int>.OnValueChangedDelegate action)
        {
            m_NetworkHealth.OnValueChanged += action;
        }

        public void RemoveListener(NetworkVariable<int>.OnValueChangedDelegate action)
        {
            m_NetworkHealth.OnValueChanged -= action;
        }

        void OnEnable()
        {
            m_NetworkHealth.OnValueChanged += HitPointsChanged;
        }

        void OnDisable()
        {
            m_NetworkHealth.OnValueChanged -= HitPointsChanged;
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > 0 && newValue <= 0)
            {
                // newly reached 0 HP
                HitPointsDepleted?.Invoke();
            }
            else if (previousValue <= 0 && newValue > 0)
            {
                // newly revived
                HitPointsReplenished?.Invoke();
            }
        }
    }
}
