using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// Simple object that keeps track of the scene loading progress of a specific instance.
    /// </summary>
    public class NetworkedLoadingProgressTracker : NetworkBehaviour
    {
        /// <summary>
        /// The current loading progress associated with the owner of this NetworkBehavior
        /// </summary>
        public NetworkVariable<float> Progress { get; } = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }
}
