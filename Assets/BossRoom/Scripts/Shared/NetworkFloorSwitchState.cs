using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

/// <summary>
/// Describes the state of a floor-switch (a/k/a "pressure plate")
/// </summary>
public class NetworkFloorSwitchState : NetworkBehaviour
{
    public NetworkVariableBool IsSwitchedOn { get; } = new NetworkVariableBool();
}
