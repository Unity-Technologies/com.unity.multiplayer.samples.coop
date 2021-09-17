using System;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Server
{
    public class ServerAnimationHandler : NetworkBehaviour
    {
        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        private VisualizationConfiguration m_VisualizationConfiguration;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public Animator animator
        {
            get => m_Animator;
            set => m_Animator = value;
        }

        public override void OnNetworkSpawn()
        {
            m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
        }

        public void SetAnimator(Animator otherAnimator)
        {
            animator = otherAnimator;
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.Alive:
                    animator.SetTrigger(m_VisualizationConfiguration.AliveStateTriggerID);
                    break;
                case LifeState.Fainted:
                    animator.SetTrigger(m_VisualizationConfiguration.FaintedStateTriggerID);
                    break;
                case LifeState.Dead:
                    animator.SetTrigger(m_VisualizationConfiguration.DeadStateTriggerID);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
            }
        }

        public override void OnNetworkDespawn()
        {
            m_NetworkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
        }
    }
}
