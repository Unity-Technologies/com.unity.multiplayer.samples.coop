using MLAPI.Serialization.Pooled;
using System;
using UnityEngine;

namespace BossRoom
{
    public enum ConnectStatus
    {
        Success,           //client successfully connected. This may also be a successful reconnect. 
        ServerFull,       //can't join, server is already at capacity. 
        MatchStarted,     //can't join, match is already in progress. 
        Unknown,          //can't join, reason unknown. 
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
        public GameObject NetworkingManagerGO;

        /// <summary>
        /// This synthesizes a general NetworkStart event out of other events provided by MLAPI. This can be removed
        /// when the NetworkingManager starts publishing this event directly. 
        /// </summary>
        public event Action NetworkStarted;

        /// <summary>
        /// This event contains the game-level results of the ApprovalCheck carried out by the server, and is fired
        /// immediately after the socket connection completing. It won't fire in the event of a socket level failure. 
        /// </summary>
        public event Action<ConnectStatus> ConnectFinished;

        /// <summary>
        /// the name of the player chosen at game start
        /// </summary>
        public string PlayerName;

        public MLAPI.NetworkingManager NetManager { get; private set; }

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            NetManager = NetworkingManagerGO.GetComponent<MLAPI.NetworkingManager>();

            //because we are not a true NetworkedBehavior, we don't get NetworkStart messages. But we still need to run at that point
            //where we know if we're a host or client. So we fake a "NetworkingManager.OnNetworkStarted" event out of the existing OnServerStarted
            //and OnClientConnectedCallback events. 
            //FIXME_DMW could this be improved?
            NetManager.OnServerStarted += NetworkStart;
            NetManager.OnClientConnectedCallback += (clientId) =>
            {
                if (clientId == NetManager.LocalClientId)
                {
                    NetworkStart();
                }
            };
        }

        private void RegisterClientMessageHandlers()
        {
            MLAPI.Messaging.CustomMessagingManager.RegisterNamedMessageHandler("S2C_ConnectResult", (senderClientId, stream) =>
            {
                using (PooledBitReader reader = PooledBitReader.Get(stream))
                {
                    ConnectStatus status = (ConnectStatus)reader.ReadInt32();

                    ConnectFinished?.Invoke(status);
                }
            });
        }

        private void RegisterServerMessageHandlers()
        {
            //TODO: plug in any C->S message handlers here. 
        }


        /// <summary>
        /// This method runs when NetworkingManager has started up (following a succesful connect on the client, or directly after StartHost is invoked
        /// on the host). It is named to match NetworkedBehaviour.NetworkStart, and serves the same role, even though GameNetPortal itself isn't a NetworkedBehaviour.
        /// </summary>
        private void NetworkStart()
        {
            if (NetManager.IsClient)
            {
                RegisterClientMessageHandlers();
            }
            if (NetManager.IsServer)
            {
                RegisterServerMessageHandlers();
            }
            if (NetManager.IsHost)
            {
                //special host code. This is what kicks off the flow that happens on a regular client
                //when it has finished connecting successfully. A dedicated server would remove this. 
                ConnectFinished?.Invoke(ConnectStatus.Success);
            }

            NetworkStarted?.Invoke();
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
            //non-portable. We need to be updated when moving to UTP transport. 
            var transport = NetworkingManagerGO.GetComponent<MLAPI.Transports.UNET.UnetTransport>();
            transport.ConnectAddress = ipaddress;
            transport.ServerListenPort = port;

            NetManager.StartHost();
        }

        /// <summary>
        /// Responsible for the Server->Client RPC's of the connection result.
        /// </summary>
        /// <param name="netId"> id of the client to send to </param>
        /// <param name="status"> the status to pass to the client</param>
        public void S2CConnectResult(ulong netId, ConnectStatus status)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteInt32((int)status);
                    MLAPI.Messaging.CustomMessagingManager.SendNamedMessage("S2C_ConnectResult", netId, stream, "MLAPI_INTERNAL");
                }
            }
        }
    }
}
