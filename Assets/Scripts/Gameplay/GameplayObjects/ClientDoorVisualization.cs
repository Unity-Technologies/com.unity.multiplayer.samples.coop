using System;
using Unity.BossRoom.Gameplay.GameState;
using Unity.BossRoom.Gameplay.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameplayObjects
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
        NetworkDoorState m_DoorState;

        [Inject]
        IPublisher<DoorStateChangedEventMessage> m_Publisher;

        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                enabled = false;
            }
            else
            {
                m_DoorState.IsOpen.OnValueChanged += OnDoorStateChanged;

                // initialize visuals based on current server state (or else we default to "closed")
                m_PhysicsObject.SetActive(!m_DoorState.IsOpen.Value);

                var gameState = FindObjectOfType<ServerBossRoomState>();
                if (gameState != null)
                {
                    gameState.Container.Inject(this);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_DoorState)
            {
                m_DoorState.IsOpen.OnValueChanged -= OnDoorStateChanged;
            }
        }

        void OnDoorStateChanged(bool wasDoorOpen, bool isDoorOpen)
        {
            m_PhysicsObject.SetActive(!isDoorOpen);
            m_Publisher?.Publish(new DoorStateChangedEventMessage() { IsDoorOpen = isDoorOpen });
        }
    }
}
