using MLAPI;
using MLAPI.NetworkedVar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes the state of a floor-switch (a/k/a "pressure plate")
/// </summary>
public class NetworkFloorSwitchState : NetworkedBehaviour
{
    public NetworkedVarBool IsSwitchedOn { get; } = new NetworkedVarBool();
}
