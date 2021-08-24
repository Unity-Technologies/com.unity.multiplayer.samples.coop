using Unity.Netcode;

/// <summary>
/// Describes the state of a floor-switch (a/k/a "pressure plate")
/// </summary>
public class NetworkFloorSwitchState : NetworkBehaviour
{
    public NetworkVariableBool IsSwitchedOn { get; } = new NetworkVariableBool();
}
