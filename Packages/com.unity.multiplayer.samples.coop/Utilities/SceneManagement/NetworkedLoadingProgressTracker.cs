using Unity.Netcode;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class NetworkedLoadingProgressTracker: NetworkBehaviour
    {
        NetworkVariable<float> m_Progress = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public float Progress
        {
            get => m_Progress.Value;
            set => m_Progress.Value = value;
        }
    }
}
