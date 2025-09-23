using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
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

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (IsServer)
            {
                OnLifeStateChanged(LifeState.Unset, m_NetworkLifeState.LifeState.Value);
                m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            }
        }

        void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.Alive:
                    if (previousValue == LifeState.Fainted)
                    {
                        NetworkAnimator.SetTrigger(m_VisualizationConfiguration.AliveStateTriggerID);
                    }
                    break;
                case LifeState.Fainted:
                    NetworkAnimator.SetTrigger(previousValue == LifeState.Unset
                        ? m_VisualizationConfiguration.EntryFaintedTriggerID
                        : m_VisualizationConfiguration.FaintedStateTriggerID);
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
            base.OnNetworkDespawn();
            if (IsServer && m_NetworkLifeState != null)
            {
                m_NetworkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            }
        }
    }
}
