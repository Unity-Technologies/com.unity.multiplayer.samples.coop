using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class NetworkPickUpState : NetworkBehaviour
    {
        public NetworkObjectReference heldNetworkObjectReference;
    }
}
