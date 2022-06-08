using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
        Reconnecting,             //client lost connection and is attempting to reconnect.
        IncompatibleBuildType,    //client build type is incompatible with server.
        HostEndedSession,         //host intentionally ended the session.
        StartHostFailed,          // server failed to bind
        StartClientFailed         // failed to connect to server and/or invalid network endpoint
    }

    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public string playerName;
        public bool isDebug;
    }

    /// <summary>
    /// This state machine handles connection through the NetworkManager. It is responsible for listening to
    /// NetworkManger callbacks and other outside calls and redirecting them to the current ConnectionState object.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        ConnectionState m_CurrentState;

        [SerializeField]
        NetworkManager m_NetworkManager;
        public NetworkManager NetworkManager => m_NetworkManager;

        [SerializeField]
        NetworkObject m_GameState;
        public NetworkObject GameState => m_GameState;

        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        [Inject]
        void InjectDependencies(IPublisher<ConnectStatus> connectStatusPublisher)
        {
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            ConnectionState.InitializeStates(this, DIScope.RootScope);

            m_CurrentState = ConnectionState.Offline;

            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
        }

        void OnDestroy()
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;

        }

        public void ChangeState(ConnectionState newState)
        {
            Debug.Log($"Changed connection sate from {m_CurrentState} to {newState}.");
            m_CurrentState.Exit();
            m_CurrentState = newState;
            m_CurrentState.Enter();
        }

        void OnClientDisconnectCallback(ulong clientId)
        {
            m_CurrentState.OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            m_CurrentState.OnClientConnected(clientId);
        }

        void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            m_CurrentState.ApprovalCheck(connectionData, clientId, connectionApprovedCallback);
        }

        public Task StartClientLobbyAsync(string playerName, Action<string> onFailure)
        {
            return m_CurrentState.StartClientLobbyAsync(playerName, onFailure);
        }

        public void StartClientIp(string playerName, string ipaddress, int port)
        {
            m_CurrentState.StartClientIP(playerName, ipaddress, port);
        }

        public void StartHostLobby(string playerName)
        {
            m_CurrentState.StartHostLobbyAsync(playerName);
        }

        public bool StartHostIp(string playerName, string ipaddress, int port)
        {
            return m_CurrentState.StartHostIP(playerName, ipaddress, port);
        }

        public void RequestShutdown()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            m_CurrentState.OnUserRequestedShutdown();
        }

        /// <summary>
        /// Registers the message handler for custom named messages. This should only be done once StartClient has been
        /// called (start client will initialize NetworkSceneManager and CustomMessagingManager)
        /// </summary>
        public void RegisterCustomMessages()
        {
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), ReceiveServerToClientSetDisconnectReason_CustomMessage);
        }

        void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);
            m_ConnectStatusPublisher.Publish(status);
        }

        /// <summary>
        /// Sends a DisconnectReason to all connected clients. This should only be done on the server, prior to disconnecting the clients.
        /// </summary>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public static void SendServerToAllClientsSetDisconnectReason(ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), writer);
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
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), clientID, writer);
        }
    }
}
