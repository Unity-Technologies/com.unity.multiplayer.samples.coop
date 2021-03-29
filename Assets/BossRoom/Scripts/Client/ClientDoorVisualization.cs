using MLAPI;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Represents a door in the client. The visuals of the door animate as
    /// "opening" and "closing", but for physics purposes this is an illusion:
    /// whenever the door is open on the server, the door's physics are disabled,
    /// and vice versa.
    /// </summary>
    [RequireComponent(typeof(NetworkDoorState))]
    public class ClientDoorVisualization : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("This physics and navmesh obstacle is enabled when the door is closed.")]
        GameObject m_PhysicsObject;

        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        string m_AnimatorDoorOpenBoolVarName = "IsOpen";

        NetworkDoorState m_DoorState;

        void Awake()
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

        void OnDestroy()
        {
            if (m_DoorState)
            {
                m_DoorState.IsOpen.OnValueChanged -= OnDoorStateChanged;
            }
        }

        void OnDoorStateChanged(bool wasDoorOpen, bool isDoorOpen)
        {
            m_PhysicsObject.SetActive(!isDoorOpen);
            m_Animator.SetBool(m_AnimatorDoorOpenBoolVarName, isDoorOpen);
        }
    }
}
