using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// MonoBehaviour containing only one NetworkVariable of type LifeState which represents this object's life state.
    /// </summary>
    public class NetworkLifeState : NetworkBehaviour, INetworkSubscribable<LifeState>
    {
        [SerializeField]
        NetworkVariable<LifeState> m_NetworkLife = new NetworkVariable<LifeState>(LifeState.Alive);

        public LifeState NetworkLife
        {
            get => m_NetworkLife.Value;
            set => m_NetworkLife.Value = value;
        }

        public void AddListener(NetworkVariable<LifeState>.OnValueChangedDelegate action)
        {
            m_NetworkLife.OnValueChanged += action;
        }

        public void RemoveListener(NetworkVariable<LifeState>.OnValueChangedDelegate action)
        {
            m_NetworkLife.OnValueChanged -= action;
        }
    }
}
