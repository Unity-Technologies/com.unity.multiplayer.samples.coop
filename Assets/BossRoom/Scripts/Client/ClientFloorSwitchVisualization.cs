using MLAPI;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Client-side representation of a floor switch.
    /// </summary>
    [RequireComponent(typeof(NetworkFloorSwitchState))]
    public class ClientFloorSwitchVisualization : NetworkBehaviour
    {
        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        string m_AnimatorPressedDownBoolVarName = "IsPressed";

        NetworkFloorSwitchState m_FloorSwitchState;

        void Awake()
        {
            m_FloorSwitchState = GetComponent<NetworkFloorSwitchState>();
        }

        public override void NetworkStart()
        {
            m_FloorSwitchState.IsSwitchedOn.OnValueChanged += OnFloorSwitchStateChanged;
        }

        void OnDestroy()
        {
            if (m_FloorSwitchState)
            {
                m_FloorSwitchState.IsSwitchedOn.OnValueChanged -= OnFloorSwitchStateChanged;
            }
        }

        void OnFloorSwitchStateChanged(bool wasPressed, bool isPressed)
        {
            m_Animator.SetBool(m_AnimatorPressedDownBoolVarName, isPressed);
        }
    }
}
