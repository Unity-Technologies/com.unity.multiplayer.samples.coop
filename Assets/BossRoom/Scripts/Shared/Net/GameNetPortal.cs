using System;
using System.Collections;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using MLAPI;
using MLAPI.Transports.LiteNetLib;
using MLAPI.Transports.PhotonRealtime;
using UnityEngine;

namespace BossRoom
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
    /// Why is there a C2S_ConnectFinished event here? How is that different from the "ApprovalCheck" logic that MLAPI optionally runs
    /// when establishing a new client connection?
    /// MLAPI's ApprovalCheck logic doesn't offer a way to return rich data. We need to know certain things directly upon logging in, such as
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
    ///
    public class GameNetPortal : MonoBehaviour
    {
        public GameObject NetworkManagerGO;

        /// <summary>
        /// This event is fired when MLAPI has reported that it has finished initialization, and is ready for
        /// business, equivalent to OnServerStarted on the server, and OnClientConnected on the client.
        /// </summary>
        public event Action NetworkReadied;

        /// <summary>
        /// This event contains the game-level results of the ApprovalCheck carried out by the server, and is fired
        /// immediately after the socket connection completing. It won't fire in the event of a socket level failure.
        /// </summary>
        public event Action<ConnectStatus> ConnectFinished;

        /// <summary>
        /// This event relays a ConnectStatus sent from the server to the client. The server will invoke this to provide extra
        /// context about an upcoming network Disconnect.
        /// </summary>
        public event Action<ConnectStatus> DisconnectReasonReceived;

        /// <summary>
        /// raised when a client has changed scenes. Returns the ClientID and the new scene the client has entered, by index.
        /// </summary>
        public event Action<ulong, int> ClientSceneChanged;

        /// <summary>
        /// This fires in response to GameNetPortal.RequestDisconnect. It's a local signal (not from the network), indicating that
        /// the user has requested a disconnect.
        /// </summary>
        public event Action UserDisconnectRequested;

        public NetworkManager NetManager { get; private set; }

        /// <summary>
        /// the name of the player chosen at game start
        /// </summary>
        public string PlayerName;

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            NetManager = NetworkManagerGO.GetComponent<NetworkManager>();

            //we synthesize a "NetworkStart" event for the NetworkManager out of existing events. At some point
            //we expect NetworkManager will expose an event like this itself.
            NetManager.OnServerStarted += OnNetworkReady;
            NetManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;

            // Start a coroutine here that will wait for CustomMessageManager to be properly initialized
            // In order to avoid a race condition in the initialization of the NetworkManager
            StartCoroutine(InitializeMessageHandlers());
        }

        private void OnDestroy()
        {
            if( NetManager != null )
            {
                NetManager.OnServerStarted -= OnNetworkReady;
                NetManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
            }

            UnregisterClientMessageHandlers();
            UnregisterServerMessageHandlers();
        }


        private void ClientNetworkReadyWrapper(ulong clientId)
        {
            if (clientId == NetManager.LocalClientId)
            {
                OnNetworkReady();
            }
        }

        IEnumerator InitializeMessageHandlers()
        {
            // Wait here in case either NetworkManager.Singleton or the CustomMessaging Manager hasn't been initialized yet
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null);

            //we register these without knowing whether we're a client or host. This is because certain messages can be sent super-early,
            //before we even get our ClientConnected event (if acting as a client). It should be harmless to have server handlers registered
            //on the client, because (a) nobody will be sending us these messages and (b) even if they did, nobody is listening for those
            //server message events on the client anyway.
            //TODO-FIXME:MLAPI Issue 799. We shouldn't really have to worry about getting messages before our ClientConnected callback.

            RegisterClientMessageHandlers();
            RegisterServerMessageHandlers();

            yield return null;
        }

        private void RegisterClientMessageHandlers()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ServerToClientConnectResult", (senderClientId, stream) =>
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    ConnectStatus status = (ConnectStatus)reader.ReadInt32();

                    ConnectFinished?.Invoke(status);
                }
            });

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ServerToClientSetDisconnectReason", (senderClientId, stream) =>
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    ConnectStatus status = (ConnectStatus)reader.ReadInt32();

                    DisconnectReasonReceived?.Invoke(status);
                }
            });
        }

        private void RegisterServerMessageHandlers()
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ClientToServerSceneChanged", (senderClientId, stream) =>
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    int sceneIndex = reader.ReadInt32();

                    ClientSceneChanged?.Invoke(senderClientId, sceneIndex);
                }

            });
        }

        private void UnregisterClientMessageHandlers()
        {
            // Early return here if the NetworkManager or the CustomMessagingManager are null
            if (NetManager == null || NetManager.CustomMessagingManager == null)
                return;

            NetManager.CustomMessagingManager.UnregisterNamedMessageHandler("ServerToClientConnectResult");
            NetManager.CustomMessagingManager.UnregisterNamedMessageHandler("ServerToClientSetDisconnectReason");
        }

        private void UnregisterServerMessageHandlers()
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ClientToServerSceneChanged");
        }


        /// <summary>
        /// This method runs when NetworkManager has started up (following a succesful connect on the client, or directly after StartHost is invoked
        /// on the host). It is named to match NetworkBehaviour.NetworkStart, and serves the same role, even though GameNetPortal itself isn't a NetworkBehaviour.
        /// </summary>
        private void OnNetworkReady()
        {
            if (NetManager.IsHost)
            {
                //special host code. This is what kicks off the flow that happens on a regular client
                //when it has finished connecting successfully. A dedicated server would remove this.
                ConnectFinished?.Invoke(ConnectStatus.Success);
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

            //DMW_NOTE: non-portable. We need to be updated when moving to UTP transport.
            switch (chosenTransport)
            {
                case LiteNetLibTransport liteNetLibTransport:
                    liteNetLibTransport.Address = ipaddress;
                    liteNetLibTransport.Port = (ushort)port;
                    break;
                case MLAPI.Transports.UNET.UNetTransport unetTransport:
                    unetTransport.ConnectAddress = ipaddress;
                    unetTransport.ServerListenPort = port;
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

        /// <summary>
        /// Responsible for the Server->Client RPC's of the connection result.
        /// </summary>
        /// <param name="netId"> id of the client to send to </param>
        /// <param name="status"> the status to pass to the client</param>
        public void ServerToClientConnectResult(ulong netId, ConnectStatus status)
        {

            using (var buffer = PooledNetworkBuffer.Get())
            {
                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteInt32((int)status);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ServerToClientConnectResult", netId, buffer, NetworkChannel.Internal);
                }
            }
        }

        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="netId"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public void ServerToClientSetDisconnectReason(ulong netId, ConnectStatus status)
        {
            using (var buffer = PooledNetworkBuffer.Get())
            {
                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteInt32((int)status);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ServerToClientSetDisconnectReason", netId, buffer, NetworkChannel.Internal);
                }
            }
        }

        public void ClientToServerSceneChanged(int newScene)
        {
            if(NetManager.IsHost)
            {
                ClientSceneChanged?.Invoke(NetManager.ServerClientId, newScene);
            }
            else if(NetManager.IsConnectedClient)
            {
                using (var buffer = PooledNetworkBuffer.Get())
                {
                    using (var writer = PooledNetworkWriter.Get(buffer))
                    {
                        writer.WriteInt32(newScene);
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ClientToServerSceneChanged", NetManager.ServerClientId, buffer, NetworkChannel.Internal);
                    }
                }
            }
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
