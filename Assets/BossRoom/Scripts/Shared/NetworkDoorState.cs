using MLAPI;
using MLAPI.NetworkedVar;
using System.Collections;
using UnityEngine;

/// <summary>
/// Network state for a door which can be opened by pressing on a floor switch.
/// </summary>
public class NetworkDoorState : NetworkedBehaviour
{
    public NetworkedVarBool IsOpen { get; } = new NetworkedVarBool();
}
