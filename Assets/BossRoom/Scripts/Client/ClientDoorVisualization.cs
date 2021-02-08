using MLAPI;
using UnityEngine;

/// <summary>
/// Represents a door in the client. The visuals of the door animate as
/// "opening" and "closing", but for physics purposes this is an illusion:
/// whenever the door is open on the server, the door's physics are disabled,
/// and vice versa.
/// </summary>
[RequireComponent(typeof(NetworkDoorState))]
public class ClientDoorVisualization : NetworkedBehaviour
{
    [SerializeField]
    [Tooltip("This physics and navmesh obstacle is enabled when the door is closed.")]
    private GameObject m_PhysicsObject;

    [SerializeField]
    private Animator m_Animator;

    [SerializeField]
    private string m_AnimatorDoorOpenBoolVarName = "IsOpen";

    private NetworkDoorState m_DoorState;

    private void Awake()
    {
        m_DoorState = GetComponent<NetworkDoorState>();
    }

    public override void NetworkStart()
    {
        if (!IsClient)
        {
            enabled = false;
        }
        else
        {
            m_DoorState.IsOpen.OnValueChanged += OnDoorStateChanged;

            // sanity-check that the physics object is active in the scene (because we default to "closed")
            m_PhysicsObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (m_DoorState)
        {
            m_DoorState.IsOpen.OnValueChanged -= OnDoorStateChanged;
        }
    }

    private void OnDoorStateChanged(bool wasDoorOpen, bool isDoorOpen)
    {
        m_PhysicsObject.SetActive(!isDoorOpen);
        m_Animator.SetBool(m_AnimatorDoorOpenBoolVarName, isDoorOpen);
    }

}
