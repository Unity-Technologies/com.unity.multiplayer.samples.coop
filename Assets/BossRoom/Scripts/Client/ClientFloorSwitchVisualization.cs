using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Client-side representation of a floor switch. For now
/// we don't try to animate the switch; we just turn on/off
/// a different "look" for on vs. off.
/// </summary>
[RequireComponent(typeof(NetworkFloorSwitchState))]
public class ClientFloorSwitchVisualization : NetworkedBehaviour
{
    [SerializeField]
    private GameObject m_PressedVisualization;
    [SerializeField]
    private GameObject m_UnpressedVisualization;

    private NetworkFloorSwitchState m_FloorSwitchState;

    private void Awake()
    {
        m_FloorSwitchState = GetComponent<NetworkFloorSwitchState>();

        // don't call Update() until NetworkStart() decides if we're IsClient or not!
        enabled = false;
    }

    public override void NetworkStart()
    {
        // allow our Update() to be called if we're actually in a Client
        enabled = IsClient; 
    }

    private void Update()
    {
        m_PressedVisualization.SetActive(m_FloorSwitchState.IsSwitchedOn.Value);
        m_UnpressedVisualization.SetActive(!m_FloorSwitchState.IsSwitchedOn.Value);
    }

}
