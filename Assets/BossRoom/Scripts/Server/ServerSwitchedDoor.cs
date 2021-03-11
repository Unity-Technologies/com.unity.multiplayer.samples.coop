using MLAPI;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-side logic for a door. This particular type of door
/// is opened when a player stands on a floor switch.
/// (Assign the floor switches for this door in the editor.)
/// </summary>
[RequireComponent(typeof(NetworkDoorState))]
public class ServerSwitchedDoor : NetworkBehaviour
{
    [SerializeField]
    public List<NetworkFloorSwitchState> m_SwitchesThatOpenThisDoor;

    private NetworkDoorState m_NetworkDoorState;

    private void Awake()
    {
        m_NetworkDoorState = GetComponent<NetworkDoorState>();

        // don't let Update() run until after NetworkStart()
        enabled = false;

        if (m_SwitchesThatOpenThisDoor.Count == 0)
            Debug.LogError("Door has no switches and can never be opened!", gameObject);
    }

    public override void NetworkStart()
    {
        enabled = IsServer;
    }

    private void Update()
    {
        bool isAnySwitchOn = false;
        foreach (var floorSwitch in m_SwitchesThatOpenThisDoor)
        {
            if (floorSwitch && floorSwitch.IsSwitchedOn.Value)
            {
                isAnySwitchOn = true;
                break;
            }
        }
        m_NetworkDoorState.IsOpen.Value = isAnySwitchOn;
    }

}
