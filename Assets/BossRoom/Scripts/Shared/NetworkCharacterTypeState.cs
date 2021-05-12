using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable which represents this character's CharacterType.
    /// </summary>
    public class NetworkCharacterTypeState : NetworkBehaviour, INetworkSubscribable<CharacterTypeEnum>
    {
        [SerializeField]
        [Tooltip("NPCs should set this value in their prefab. For players, this value is set at runtime.")]
        NetworkVariable<CharacterTypeEnum> m_NetworkCharacterType = new NetworkVariable<CharacterTypeEnum>();

        public CharacterTypeEnum NetworkCharacterType
        {
            get => m_NetworkCharacterType.Value;
            set => m_NetworkCharacterType.Value = value;
        }

        public void AddListener(NetworkVariable<CharacterTypeEnum>.OnValueChangedDelegate action)
        {
            m_NetworkCharacterType.OnValueChanged += action;
        }

        public void RemoveListener(NetworkVariable<CharacterTypeEnum>.OnValueChangedDelegate action)
        {
            m_NetworkCharacterType.OnValueChanged -= action;
        }
    }
}
