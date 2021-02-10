using MLAPI;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossRoom
{
    public class ClientSpawnerVisualization : NetworkedBehaviour
    {
        [FormerlySerializedAs("m_NetworkSpawnerState")]
        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        // TODO: Integrate visuals (GOMPS-123)
        [SerializeField]
        Animator m_Animator;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
                return;
            }

            m_NetworkHealthState.HitPointsDepleted += SpawnerHitPointsDepleted;
            m_NetworkHealthState.HitPointsReplenished += SpawnerHitPointsReplenished;
            m_NetworkHealthState.HitPoints.OnValueChanged += HitPointsChanged;
        }

        void SpawnerHitPointsDepleted()
        {
            // TODO: Integrate visuals (GOMPS-123)
        }

        void SpawnerHitPointsReplenished()
        {
            // TODO: Integrate visuals (GOMPS-123)
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > newValue && newValue > 0)
            {
                // received a hit
            }
        }
    }
}
