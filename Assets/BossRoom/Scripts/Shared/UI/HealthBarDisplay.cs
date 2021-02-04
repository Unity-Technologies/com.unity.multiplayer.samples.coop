using System;
using System.Collections;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    [RequireComponent(typeof(NetworkHealthState))]
    public class HealthBarDisplay : NetworkedBehaviour
    {
        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        [Tooltip("Health bar will appear where this transform is. If null this transform is used.")]
        [SerializeField]
        Transform m_TransformToTrack;

        // as soon as any HP goes to 0, we wait this long before removing health bar UI object
        const float k_DurationSeconds = 2f;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                return;
            }

            HealthBarManager.Instance.AddHealthState(NetworkedObject.NetworkId,
                m_NetworkHealthState, m_TransformToTrack);
            m_NetworkHealthState.HitPoints.OnValueChanged += HitPointsChanged;
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > 0)
            {
                if (newValue <= 0)
                {
                    // newly reached 0 HP
                    StartCoroutine(WaitToHideHealthBar());
                }
            }
            else
            {
                if (newValue > 0)
                {
                    // newly revived
                    HealthBarManager.Instance.AddHealthState(NetworkedObject.NetworkId, m_NetworkHealthState,
                        m_TransformToTrack);
                }
            }
        }

        IEnumerator WaitToHideHealthBar()
        {
            yield return new WaitForSeconds(k_DurationSeconds);

            HealthBarManager.Instance.Remove(NetworkedObject.NetworkId);
        }
    }
}
