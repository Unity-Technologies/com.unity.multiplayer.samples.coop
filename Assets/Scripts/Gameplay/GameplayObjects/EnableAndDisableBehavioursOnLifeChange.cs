using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    [RequireComponent(typeof(NetworkLifeState))]
    public class EnableAndDisableBehavioursOnLifeChange : MonoBehaviour
    {
        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        Behaviour[] m_BehavioursToEnableOnAlive;

        [SerializeField]
        Behaviour[] m_BehavioursToDisableOnNotAlive;

        void Awake()
        {
            m_NetworkLifeState.LifeState.OnValueChanged += OnValueChanged;
        }

        void OnValueChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue == LifeState.Alive)
            {
                foreach (var monoBehaviourToEnable in m_BehavioursToEnableOnAlive)
                {
                    monoBehaviourToEnable.enabled = true;
                }
            }
            else
            {
                foreach (var monoBehaviourToDisable in m_BehavioursToDisableOnNotAlive)
                {
                    monoBehaviourToDisable.enabled = false;
                }
            }
        }

        void OnDestroy()
        {
            if (m_NetworkLifeState)
            {
                m_NetworkLifeState.LifeState.OnValueChanged -= OnValueChanged;
            }
        }
    }
}
