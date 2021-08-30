using Unity.Netcode;

/// <summary>
/// Network state for a door which can be opened by pressing on a floor switch.
/// </summary>
public class NetworkDoorState : NetworkBehaviour
{
    public NetworkVariable<bool> IsOpen { get; } = new NetworkVariable<bool>();
}
