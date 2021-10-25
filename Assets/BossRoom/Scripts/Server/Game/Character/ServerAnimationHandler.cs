using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerAnimationHandler : NetworkBehaviour
    {
        [SerializeField]
        NetworkAnimator m_NetworkAnimator;

        [SerializeField]
        private VisualizationConfiguration m_VisualizationConfiguration;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public NetworkAnimator NetworkAnimator
        {
            get => m_NetworkAnimator;
            set => m_NetworkAnimator = value;
        }

        public override void OnNetworkSpawn()
        {
            m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.Alive:
                    NetworkAnimator.SetTrigger(m_VisualizationConfiguration.AliveStateTriggerID);
                    break;
                case LifeState.Fainted:
                    NetworkAnimator.SetTrigger(m_VisualizationConfiguration.FaintedStateTriggerID);
                    break;
                case LifeState.Dead:
                    NetworkAnimator.SetTrigger(m_VisualizationConfiguration.DeadStateTriggerID);
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
