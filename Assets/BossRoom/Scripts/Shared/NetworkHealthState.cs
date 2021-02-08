using System;
using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkedBehaviour containing only one NetworkedVarInt which represents this object's health.
    /// </summary>
    public class NetworkHealthState : NetworkedBehaviour
    {
        [HideInInspector]
        public NetworkedVarInt HitPoints;

        // public subscribable event to be invoked when HP has been fully depleted
        public event Action HitPointsDepleted;

        // public subscribable event to be invoked when HP has been replenished
        public event Action HitPointsReplenished;

        public override void NetworkStart()
        {
            HitPoints.OnValueChanged += HitPointsChanged;
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > 0)
            {
                if (newValue <= 0)
                {
                    // newly reached 0 HP
                    if (HitPointsDepleted != null)
                    {
                        HitPointsDepleted.Invoke();
                    }
                }
            }
            else
            {
                if (newValue > 0)
                {
                    // newly revived
                    if (HitPointsReplenished != null)
                    {
                        HitPointsReplenished.Invoke();
                    }
                }
            }
        }
    }
}
