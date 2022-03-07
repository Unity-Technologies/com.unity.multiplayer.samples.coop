using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Represents a door in the client. The visuals of the door animate as
    /// "opening" and "closing", but for physics purposes this is an illusion:
    /// whenever the door is open on the server, the door's physics are disabled,
    /// and vice versa.
    /// </summary>
    [RequireComponent(typeof(NetworkDoorState))]
    [RequireComponent(typeof(OnSpawnBehaviourEnabler))]
    public class ClientDoorVisualization : MonoBehaviour, IClientOnlyMonoBehaviour
    {
        [SerializeField]
        [Tooltip("This physics and navmesh obstacle is enabled when the door is closed.")]
        GameObject m_PhysicsObject;

        [SerializeField]
        NetworkDoorState m_DoorState;

        void OnDoorStateChanged(bool wasDoorOpen, bool isDoorOpen)
        {
            m_PhysicsObject.SetActive(!isDoorOpen);
        }

        public void SetEnabled(bool enable)
        {
            enabled = enable;
            if (enable)
            {
                m_DoorState.IsOpen.OnValueChanged += OnDoorStateChanged;

                // initialize visuals based on current server state (or else we default to "closed")
                OnDoorStateChanged(false, m_DoorState.IsOpen.Value);
            }
            else
            {
                m_DoorState.IsOpen.OnValueChanged -= OnDoorStateChanged;
            }
        }
    }
}
