using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// MonoBehaviour containing only one NetworkVariable of type LifeState which represents this object's life state.
    /// </summary>
    public class NetworkLifeState : NetworkBehaviour
    {
        [SerializeField]
        NetworkVariable<LifeState> m_LifeState = new NetworkVariable<LifeState>(BossRoom.LifeState.Alive);

        public NetworkVariable<LifeState> LifeState => m_LifeState;

        /// <summary>
        /// Indicates whether this character is in "god mode" (cannot be damaged).
        /// </summary>
        public NetworkVariable<bool> IsGodMode { get; } = new NetworkVariable<bool>(false);
    }
}
