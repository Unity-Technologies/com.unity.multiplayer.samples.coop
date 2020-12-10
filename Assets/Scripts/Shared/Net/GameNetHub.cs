using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    public enum ConnectStatus
    {
        SUCCESS,           //client successfully connected. This may also be a successful reconnect. 
        ESERVERFULL,       //can't join, server is already at capacity. 
        EMATCHSTARTED,     //can't join, match is already in progress. 
        EUNKNOWN           //can't join, reason unknown. 
    }

    /// <summary>
    /// The GameNetHub is a general-purpose relay for game network messages between the client and server. It is available
    /// as soon as the initial network connection has completed, and persists across all scenes. Its purpose is to move non-GameObject-specific
    /// methods between server and client. Generally these have to do with connection, and match end conditions. 
    /// </summary>
    /// 
    /// <remarks
    /// Why is there a RequestConnect call-and-response here? How is that different from the "ApprovalCheck" logic that MLAPI optionally runs
    /// when establishing a new client connection? 
    /// In short, the connection flow in this class happens second after the logic embodied by StartClient runs (and after the client has seen
    /// NetworkStart fire). We need to provide an initial dump of info when a client logs in, specifically what scene to transition to. We would need
    /// this message even if we used ConnectionData to send up our client GUID--because of that, it makes more sense to keep the two RPCs here, as
    /// a 2nd step in the login flow. 
    /// 
    /// Why do we need to send a client GUID? What is it? Don't we already have a clientID? 
    /// ClientIDs are assigned on login. If you connect to a server, then your connection drops, and you reconnect, you get a new ClientID. This
    /// makes it awkward to get back your old character, which the server is going to hold onto for a fixed timeout. To properly reconnect and recover
    /// your character, you need a persistent identifier for your own client install. We solve that by generating a random GUID and storing it
    /// in player prefs, so it persists across sessions of the game. 
    /// </remarks>
    /// 
    public class GameNetHub : MLAPI.NetworkedBehaviour
    {
        public GameObject NetworkingManagerGO;

        private BossRoomClient.GNH_Client m_clientLogic;
        private BossRoomServer.GNH_Server m_serverLogic;

        public MLAPI.NetworkingManager NetManager { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            Object.DontDestroyOnLoad(this.gameObject);
            Object.DontDestroyOnLoad(NetworkingManagerGO);

            NetManager = NetworkingManagerGO.GetComponent<MLAPI.NetworkingManager>();
        }

        public override void NetworkStart()
        {
            if (NetManager.IsClient)
            {
                m_clientLogic = new BossRoomClient.GNH_Client(this);
            }
            if ( NetManager.IsServer )
            {
                m_serverLogic = new BossRoomServer.GNH_Server(this);
                
                //special host code. This is what kicks off the flow that happens on a regular client
                //when it has finished connecting successfully. A dedicated server would remove this. 
                RecvConnectFinished(ConnectStatus.SUCCESS, BossRoomState.CHARSELECT);
            }
        }

        /// <summary>
        /// Wraps the invocation of NetworkingManager.StartClient, including our GUID as the payload. 
        /// </summary>
        /// <param name="ipaddress">the IP address of the host to connect to.</param>
        /// <param name="port">The port of the host to connect to. </param>
        public void StartClient(string ipaddress, int port)
        {
            BossRoomClient.GNH_Client.StartClient(this, ipaddress, port);
        }

        //Server->Client RPCs

        public void S2C_ConnectResult( ulong netId, ConnectStatus status, BossRoomState targetState )
        {
            InvokeClientRpcOnClient("RecvConnectResult", 
                netId, 
                "MLAPI_INTERNAL",                             // channelID. Must be MLAPI_INTERNAL Because it is called as part of the StartClient flow (before possible reject comes back). 
                MLAPI.Security.SecuritySendFlags.None, 
                MLAPI.Security.SecuritySendFlags.None, 
                status,                                       // this is the actual payload
                targetState);                                 // ""
        }
        [MLAPI.Messaging.ClientRPC()]
        private void RecvConnectFinished( ConnectStatus status, BossRoomState targetState )
        {
            m_clientLogic.RecvConnectFinished( status, targetState );
        }

    }
}
