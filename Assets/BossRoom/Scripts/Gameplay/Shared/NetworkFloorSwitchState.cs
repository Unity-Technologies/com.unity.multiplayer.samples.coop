using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Describes the state of a floor-switch (a/k/a "pressure plate")
    /// </summary>
    public class NetworkFloorSwitchState : NetworkBehaviour
    {
        public NetworkVariable<bool> IsSwitchedOn { get; } = new NetworkVariable<bool>();
    }
}
