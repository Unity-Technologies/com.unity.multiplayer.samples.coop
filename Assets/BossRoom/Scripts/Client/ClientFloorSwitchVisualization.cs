using MLAPI;
using UnityEngine;

/// <summary>
/// Client-side representation of a floor switch.
/// </summary>
[RequireComponent(typeof(NetworkFloorSwitchState))]
public class ClientFloorSwitchVisualization : NetworkBehaviour
{
    [SerializeField]
    private Animator m_Animator;

    [SerializeField]
    private string m_AnimatorPressedDownBoolVarName = "IsPressed";

    private NetworkFloorSwitchState m_FloorSwitchState;

    private void Awake()
    {
        m_FloorSwitchState = GetComponent<NetworkFloorSwitchState>();
    }

    public override void NetworkStart()
    {
        m_FloorSwitchState.IsSwitchedOn.OnValueChanged += OnFloorSwitchStateChanged;
    }

    private void OnDestroy()
    {
        if (m_FloorSwitchState)
        {
            m_FloorSwitchState.IsSwitchedOn.OnValueChanged -= OnFloorSwitchStateChanged;
        }
    }

    private void OnFloorSwitchStateChanged(bool wasPressed, bool isPressed)
    {
        m_Animator.SetBool(m_AnimatorPressedDownBoolVarName, isPressed);
    }

}
