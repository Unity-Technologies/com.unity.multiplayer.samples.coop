using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public enum ConnectionStateType
    {
        Offline,
        Connecting,
        Connected,
        Reconnecting,
        Hosting
    }

    public class ConnectionManager : MonoBehaviour
    {
        static readonly ConnectionStateType[] k_ConnectionStates = (ConnectionStateType[])Enum.GetValues(typeof(ConnectionStateType));
        ConnectionStateType m_CurrentState;
        Dictionary<ConnectionStateType, ConnectionState> m_Logics;

        [SerializeField]
        NetworkManager m_NetworkManager;
        public NetworkManager NetworkManager => m_NetworkManager;

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;
        public AvatarRegistry AvatarRegistry => m_AvatarRegistry;

        [SerializeField]
        NetworkObject m_GameState;
        public NetworkObject GameState => m_GameState;

        DisconnectReason m_DisconnectReason = new DisconnectReason();
        public DisconnectReason DisconnectReason => m_DisconnectReason;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            m_Logics = new Dictionary<ConnectionStateType, ConnectionState>()
            {
                [ConnectionStateType.Offline] = new OfflineConnectionState(this),
                [ConnectionStateType.Connecting] = new ConnectingConnectionState(this),
                [ConnectionStateType.Connected] = new ConnectedConnectionState(this),
                [ConnectionStateType.Reconnecting] = new ReconnectingConnectionState(this),
                [ConnectionStateType.Hosting] = new HostingConnectionState(this)
            };
            m_CurrentState = ConnectionStateType.Offline;

            var scope = DIScope.RootScope;
            foreach (var connectionState in m_Logics.Values)
            {
                scope.InjectIn(connectionState);
            }

            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
        }

        void OnDestroy()
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;

        }

        public void ChangeState(ConnectionStateType newState)
        {
            Debug.Log($"Changed connection sate from {m_CurrentState} to {newState}.");
            m_Logics[m_CurrentState].Exit();
            m_CurrentState = newState;
            m_Logics[m_CurrentState].Enter();
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            m_Logics[m_CurrentState].OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            m_Logics[m_CurrentState].OnClientConnected(clientId);
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalCallback" exposed by Netcode.NetworkManager, and is run every time a client connects to us.
        /// See ClientGameNetPortal.StartClient for the complementary logic that runs when the client starts its connection.
        /// </summary>
        /// <remarks>
        /// Since our game doesn't have to interact with some third party authentication service to validate the identity of the new connection, our ApprovalCheck
        /// method is simple, and runs synchronously, invoking "callback" to signal approval at the end of the method. Netcode currently doesn't support the ability
        /// to send back more than a "true/false", which means we have to work a little harder to provide a useful error return to the client. To do that, we invoke a
        /// custom message in the same channel that Netcode uses for its connection callback. Since the delivery is NetworkDelivery.ReliableSequenced, we can be
        /// confident that our login result message will execute before any disconnect message.
        /// </remarks>
        /// <param name="connectionData">binary data passed into StartClient. In our case this is the client's GUID, which is a unique identifier for their install of the game that persists across app restarts. </param>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="connectionApprovedCallback">The delegate we must invoke to signal that the connection was approved or not. </param>
        void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            m_Logics[m_CurrentState].ApprovalCheck(connectionData, clientId, connectionApprovedCallback);
        }

        public Task StartClientLobbyAsync(string playerName, Action<string> onFailure)
        {
            return m_Logics[m_CurrentState].StartClientLobbyAsync(playerName, onFailure);
        }

        public void StartClientIp(string playerName, string ipaddress, int port)
        {
            m_Logics[m_CurrentState].StartClientIP(playerName, ipaddress, port);
        }

        public void StartHostLobby(string playerName)
        {
            m_Logics[m_CurrentState].StartHostLobbyAsync(playerName);
        }

        public bool StartHostIp(string playerName, string ipaddress, int port)
        {
            return m_Logics[m_CurrentState].StartHostIP(playerName, ipaddress, port);
        }

        public void RequestShutdown()
        {
            DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);
            m_Logics[m_CurrentState].OnUserRequestedShutdown();
        }

        public void RegisterCustomMessages()
        {
            // should only do this once StartClient has been called (start client will initialize NetworkSceneManager and CustomMessagingManager)
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), ReceiveServerToClientSetDisconnectReason_CustomMessage);
        }

        void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);
            m_DisconnectReason.SetDisconnectReason(status);
        }

        /// <summary>
        /// Sends a DisconnectReason to all connected clients. This should only be done on the server, prior to disconnecting the clients.
        /// </summary>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public static void SendServerToAllClientsSetDisconnectReason(ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(nameof(ClientGameNetPortal.ReceiveServerToClientSetDisconnectReason_CustomMessage), writer);
        }

        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public static void SendServerToClientSetDisconnectReason(ulong clientID, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientSetDisconnectReason_CustomMessage), clientID, writer);
        }
    }
}
