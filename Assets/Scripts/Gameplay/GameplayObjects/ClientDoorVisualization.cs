using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Represents a door in the client. The visuals of the door animate as
    /// "opening" and "closing", but for physics purposes this is an illusion:
    /// whenever the door is open on the server, the door's physics are disabled,
    /// and vice versa.
    /// </summary>
    [RequireComponent(typeof(NetworkDoorState))]
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientDoorVisualization : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("This physics and navmesh obstacle is enabled when the door is closed.")]
        GameObject m_PhysicsObject;

        [SerializeField]
        NetworkDoorState m_DoorState;

        NetcodeHooks m_Hooks;
        [Inject]
        IPublisher<DoorStateChangedEventMessage> m_Publisher;

        void Awake()
        {
            m_Hooks = GetComponent<NetcodeHooks>();
            m_Hooks.OnNetworkSpawnHook += OnSpawn;
            m_Hooks.OnNetworkDespawnHook += OnDespawn;
        }

        void OnSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                enabled = false;
            }
            else
            {
                m_DoorState.IsOpen.OnValueChanged += OnDoorStateChanged;

                // initialize visuals based on current server state (or else we default to "closed")
                m_PhysicsObject.SetActive(!m_DoorState.IsOpen.Value);

                var gameState = FindObjectOfType<ClientBossRoomState>();
                if (gameState != null)
                {
                    gameState.Container.Inject(this);
                }
            }
        }

        void OnDespawn()
        {
            if (m_DoorState)
            {
                m_DoorState.IsOpen.OnValueChanged -= OnDoorStateChanged;
            }
        }

        public void OnDestroy()
        {
            m_Hooks.OnNetworkSpawnHook -= OnSpawn;
            m_Hooks.OnNetworkDespawnHook -= OnDespawn;
        }

        void OnDoorStateChanged(bool wasDoorOpen, bool isDoorOpen)
        {
            m_PhysicsObject.SetActive(!isDoorOpen);
            m_Publisher?.Publish(new DoorStateChangedEventMessage() { IsDoorOpen = isDoorOpen });
        }
    }
}
