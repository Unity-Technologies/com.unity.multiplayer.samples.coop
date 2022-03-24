using Unity.Netcode;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// Simple object that keeps track of the scene loading progress of a specific instance
    /// </summary>
    public class NetworkedLoadingProgressTracker: NetworkBehaviour
    {
        NetworkVariable<float> m_Progress = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> Progress => m_Progress;

    }
}
