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
        VisualizationConfiguration m_VisualizationConfiguration;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public NetworkAnimator NetworkAnimator => m_NetworkAnimator;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
                if (m_NetworkLifeState.LifeState.Value != LifeState.Alive)
                {
                    // If the character is not alive at spawn, send the appropriate animation trigger.
                    OnLifeStateChanged(LifeState.Alive, m_NetworkLifeState.LifeState.Value);
                }
            }
        }

        void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
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
            if (IsServer)
            {
                m_NetworkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            }
        }
    }
}
