using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable which represents this character's appearance.
    /// </summary>
    public class NetworkAppearanceState : NetworkBehaviour, INetworkSubscribable<int>
    {
        /// <summary>
        /// This is an int rather than an enum because it is a "place-marker" for a more complicated system. Ultimately we would like
        /// PCs to represent their appearance via a struct of appearance options (so they can mix-and-match different ears, head, face, etc).
        /// </summary>
        [Tooltip("Value between 0-7. ClientCharacterVisualization will use this to set up the model (for PCs).")]
        [SerializeField]
        NetworkVariableInt m_NetworkCharacterAppearance = new NetworkVariableInt();

        public int NetworkCharacterAppearance
        {
            get => m_NetworkCharacterAppearance.Value;
            set => m_NetworkCharacterAppearance.Value = value;
        }

        public void AddListener(NetworkVariable<int>.OnValueChangedDelegate action)
        {
            m_NetworkCharacterAppearance.OnValueChanged += action;
        }

        public void RemoveListener(NetworkVariable<int>.OnValueChangedDelegate action)
        {
            m_NetworkCharacterAppearance.OnValueChanged -= action;
        }
    }
}
