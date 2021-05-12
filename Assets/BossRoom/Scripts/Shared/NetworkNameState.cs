using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariableString which represents this object's name.
    /// </summary>
    public class NetworkNameState : NetworkBehaviour, INetworkSubscribable<string>
    {
        [SerializeField]
        NetworkVariableString m_NetworkName = new NetworkVariableString();

        public string NetworkName
        {
            get => m_NetworkName.Value;
            set => m_NetworkName.Value = value;
        }

        public void AddListener(NetworkVariable<string>.OnValueChangedDelegate action)
        {
            m_NetworkName.OnValueChanged += action;
        }

        public void RemoveListener(NetworkVariable<string>.OnValueChangedDelegate action)
        {
            m_NetworkName.OnValueChanged -= action;
        }
    }
}
