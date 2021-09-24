using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// MonoBehaviour containing only one NetworkVariableInt which represents this object's health.
    /// </summary>
    public class NetworkHealthState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> HitPoints = new NetworkVariable<int>();

        // public subscribable event to be invoked when HP has been fully depleted
        public event Action hitPointsDepleted;

        // public subscribable event to be invoked when HP has been replenished
        public event Action hitPointsReplenished;

        void OnEnable()
        {
            HitPoints.OnValueChanged += HitPointsChanged;
        }

        void OnDisable()
        {
            HitPoints.OnValueChanged -= HitPointsChanged;
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > 0 && newValue <= 0)
            {
                // newly reached 0 HP
                hitPointsDepleted?.Invoke();
            }
            else if (previousValue <= 0 && newValue > 0)
            {
                // newly revived
                hitPointsReplenished?.Invoke();
            }
        }
    }
}
