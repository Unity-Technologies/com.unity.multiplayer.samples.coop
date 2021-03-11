using MLAPI;
using MLAPI.NetworkVariable;
using System.Collections;
using UnityEngine;

/// <summary>
/// Network state for a door which can be opened by pressing on a floor switch.
/// </summary>
public class NetworkDoorState : NetworkBehaviour
{
    public NetworkVariableBool IsOpen { get; } = new NetworkVariableBool();
}
