using System;
using System.Collections;
using MLAPI.Transports.LiteNetLib;
using MLAPI.Transports.PhotonRealtime;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;

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
    }

    public enum OnlineMode
    {
        IpHost = 0, // The server is hosted directly and clients can join by ip address.
        Relay = 1, // The server is hosted over a relay server and clients join by entering a room name.
        UnityRelay = 2, // The server is hosted over a Unity Relay server and clients join by entering a join code.
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string clientGUID;
        public int clientScene = -1;
        public string playerName;
    }

    /// <summary>
    /// The GameNetPortal is the general purpose entry-point for game network messages between the client and server. It is available
    /// as soon as the initial network connection has completed, and persists across all scenes. Its purpose is to move non-GameObject-specific
    /// methods between server and client. Generally these have to do with connection, and match end conditions.
    /// </summary>
    ///
    /// <remarks>
    /// Why is there a C2S_ConnectFinished event here? How is that different from the "ApprovalCheck" logic that Netcode
    /// for GameObjects (Netcode) optionally runs when establishing a new client connection?
    /// Netcode's ApprovalCheck logic doesn't offer a way to return rich data. We need to know certain things directly upon logging in, such as
    /// whether the game-layer even wants us to join (we could fail because the server is full, or some other non network related reason), and also
    /// what BossRoomState to transition to. We do this with a Custom Named Message, which fires on the server immediately after the approval check delegate
    /// has run.
    ///
    /// Why do we need to send a client GUID? What is it? Don't we already have a clientID?
    /// ClientIDs are assigned on login. If you connect to a server, then your connection drops, and you reconnect, you get a new ClientID. This
    /// makes it awkward to get back your old character, which the server is going to hold onto for a fixed timeout. To properly reconnect and recover
    /// your character, you need a persistent identifier for your own client install. We solve that by generating a random GUID and storing it
    /// in player prefs, so it persists across sessions of the game.
    /// </remarks>
    // todo this should be refactored to 2 classes and should be renamed connection manager or something more clear like this.
    public class GameNetPortal : MonoBehaviour
    {
        [SerializeField]
        NetworkManager m_NetworkManager;

        /// <summary>
        /// This event is fired when Netcode has reported that it has finished initialization, and is ready for
        /// business, equivalent to OnServerStarted on the server, and OnClientConnected on the client.
        /// </summary>
        public event Action NetworkReadied;

        /// <summary>
        /// raised when a client has changed scenes. Returns the ClientID and the new scene the client has entered, by index.
        /// </summary>
        public event Action<ulong, int> ClientSceneChanged;

        /// <summary>
        /// This fires in response to GameNetPortal.RequestDisconnect. It's a local signal (not from the network), indicating that
        /// the user has requested a disconnect.
        /// </summary>
        public event Action UserDisconnectRequested;

        public NetworkManager NetManager => m_NetworkManager;

        /// <summary>
        /// the name of the player chosen at game start
        /// </summary>
        public string PlayerName;

        /// <summary>
        /// How many connections we create a Unity relay allocation for
        /// </summary>
        private const int k_MaxUnityRelayConnections = 8;

        // Instance of GameNetPortal placed in scene. There should only be one at once
        public static GameNetPortal Instance;
        private ClientGameNetPortal m_ClientPortal;

        private void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            m_ClientPortal = GetComponent<ClientGameNetPortal>();
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            //we synthesize a "OnNetworkSpawn" event for the NetworkManager out of existing events. At some point
            //we expect NetworkManager will expose an event like this itself.
            NetManager.OnServerStarted += OnNetworkReady;
            NetManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventData.SceneEventTypes.C2S_LoadComplete) return;
            ClientSceneChanged?.Invoke(sceneEvent.ClientId, SceneManager.GetSceneByName(sceneEvent.SceneName).buildIndex);
        }

        private void OnDestroy()
        {
            if (NetManager != null)
            {
                NetManager.OnServerStarted -= OnNetworkReady;
                NetManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
            }

            Instance = null;
        }

        private void ClientNetworkReadyWrapper(ulong clientId)
        {
            if (clientId == NetManager.LocalClientId)
            {
                OnNetworkReady();
                NetManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }


        // private abstract class ConnectResultMessage : INetworkMessage
        // {
        //     public ConnectStatus status;
        //
        //     public static void Receive(FastBufferReader reader, in NetworkContext context) {}
        //
        //     public void Serialize(FastBufferWriter writer)
        //     {
        //         writer.WriteValueSafe(status);
        //     }
        // }
        //
        // private class ServerToClientConnectResult : ConnectResultMessage
        // {
        //     public new static void Receive(FastBufferReader reader, in NetworkContext context)
        //     {
        //         reader.ReadValueSafe(out ConnectStatus status);
        //         Instance.ConnectFinished?.Invoke(status);
        //     }
        // }
        //
        // private class ServerToClientSetDisconnectReason : ConnectResultMessage
        // {
        //     public new static void Receive(FastBufferReader reader, in NetworkContext context)
        //     {
        //         reader.ReadValueSafe(out ConnectStatus status);
        //         Instance.DisconnectReasonReceived?.Invoke(status);
        //     }
        // }

        /// <summary>
        /// This method runs when NetworkManager has started up (following a succesful connect on the client, or directly after StartHost is invoked
        /// on the host). It is named to match NetworkBehaviour.OnNetworkSpawn, and serves the same role, even though GameNetPortal itself isn't a NetworkBehaviour.
        /// </summary>
        private void OnNetworkReady()
        {
            if (NetManager.IsHost)
            {
                //special host code. This is what kicks off the flow that happens on a regular client
                //when it has finished connecting successfully. A dedicated server would remove this.
                m_ClientPortal.OnConnectFinished(ConnectStatus.Success);
                // ConnectFinished?.Invoke(ConnectStatus.Success);
            }

            NetworkReadied?.Invoke();
        }

        /// <summary>
        /// Initializes host mode on this client. Call this and then other clients should connect to us!
        /// </summary>
        /// <remarks>
        /// See notes in GNH_Client.StartClient about why this must be static.
        /// </remarks>
        /// <param name="ipaddress">The IP address to connect to (currently IPV4 only).</param>
        /// <param name="port">The port to connect to. </param>
        public void StartHost(string ipaddress, int port)
        {
            var chosenTransport  = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().IpHostTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            // Note: In most cases, this switch case shouldn't be necessary. It becomes necessary when having to deal with multiple transports like this
            // sample does, since current Transport API doesn't expose these fields.
            switch (chosenTransport)
            {
                case LiteNetLibTransport liteNetLibTransport:
                    liteNetLibTransport.Address = ipaddress;
                    liteNetLibTransport.Port = (ushort)port;
                    break;
                case UNetTransport unetTransport:
                    unetTransport.ConnectAddress = ipaddress;
                    unetTransport.ServerListenPort = port;
                    break;
                case UnityTransport unityTransport:
                    unityTransport.SetConnectionData(ipaddress, (ushort) port);
                    // UnityTransport. = ipaddress;
                    // UnityTransport. = (ushort)port;
                    break;
                default:
                    throw new Exception($"unhandled IpHost transport {chosenTransport.GetType()}");
            }

            NetManager.StartHost();
        }

        public void StartRelayHost(string roomName)
        {
            var chosenTransport  = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().RelayTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            switch (chosenTransport)
            {
                case PhotonRealtimeTransport photonRealtimeTransport:
                    photonRealtimeTransport.RoomName = roomName;
                    break;
                default:
                    throw new Exception($"unhandled relay transport {chosenTransport.GetType()}");
            }

            NetManager.StartHost();
        }

        public async void StartUnityRelayHost()
        {
            var chosenTransport  = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().UnityRelayTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            switch (chosenTransport)
            {
                case UnityTransport utp:
                    Debug.Log("Setting up Unity Relay host");

                    try
                    {
                        await UnityServices.InitializeAsync();
                        if (!AuthenticationService.Instance.IsSignedIn)
                        {
                            await AuthenticationService.Instance.SignInAnonymouslyAsync();
                            var playerId = AuthenticationService.Instance.PlayerId;
                            Debug.Log(playerId);
                        }

                        // we now need to get the joinCode?
                        var serverRelayUtilityTask =
                            RelayUtility.AllocateRelayServerAndGetJoinCode(k_MaxUnityRelayConnections);
                        await serverRelayUtilityTask;
                        // we now have the info from the relay service
                        var (ipv4Address, port, allocationIdBytes, connectionData, key, joinCode) =
                            serverRelayUtilityTask.Result;

                        RelayJoinCode.Code = joinCode;

                        // we now need to set the RelayCode somewhere :P
                        utp.SetRelayServerData(ipv4Address, port, allocationIdBytes, key, connectionData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat($"{e.Message}");
                        throw;
                    }

                    break;
                default:
                    throw new Exception($"unhandled relay transport {chosenTransport.GetType()}");
            }

            NetManager.StartHost();
        }

        /// <summary>
        /// Responsible for the Server->Client custom message of the connection result.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> the status to pass to the client</param>
        public void SendServerToClientConnectResult(ulong clientID, ConnectStatus status)
        {
            // todo add jira bug, null ref if you use default constructor for FastBufferWriter
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientConnectResult), clientID, writer);
        }


        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public void SendServerToClientSetDisconnectReason(ulong clientID, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientSetDisconnectReason), clientID, writer);
        }


        /// <summary>
        /// This will disconnect (on the client) or shutdown the server (on the host).
        /// </summary>
        public void RequestDisconnect()
        {
            UserDisconnectRequested?.Invoke();
        }
    }
}
