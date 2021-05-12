using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    public enum WinState
    {
        Invalid,
        Win,
        Loss
    }

    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable which represents this object's win state.
    /// </summary>
    public class NetworkWinState : NetworkBehaviour, INetworkSubscribable<WinState>
    {
        [SerializeField]
        NetworkVariable<WinState> m_NetworkWin = new NetworkVariable<WinState>(WinState.Invalid);

        public WinState NetworkWin
        {
            get => m_NetworkWin.Value;
            set => m_NetworkWin.Value = value;
        }

        public void AddListener(NetworkVariable<WinState>.OnValueChangedDelegate action)
        {
            m_NetworkWin.OnValueChanged += action;
        }

        public void RemoveListener(NetworkVariable<WinState>.OnValueChangedDelegate action)
        {
            m_NetworkWin.OnValueChanged -= action;
        }
    }
}
