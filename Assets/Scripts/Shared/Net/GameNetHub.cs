using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    public enum ConnectStatus
    {
        CONNECT,           //client successfully connected. This may also be a successful reconnect. 
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

        private GNH_Client m_clientLogic;
        private GNH_Server m_serverLogic;

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
            if( NetManager.IsServer )
            {
                m_serverLogic = new GNH_Server(this);
            }
            else if( NetManager.IsClient )
            {
                m_clientLogic = new GNH_Client(this);
            }
            else
            {
                Debug.LogError("NetworkStart invoked, but NetworkingManager is neither server nor client");
            }
        }


        //Server->Client RPCs

        public void S2C_ConnectResult( ulong netId, ConnectStatus status )
        {
            InvokeClientRpcOnClient("RecvConnectResult", netId, status);
        }
        [MLAPI.Messaging.ClientRPC]
        private void RecvConnectFinished( ConnectStatus status )
        {
            m_clientLogic.RecvConnectFinished( status);
        }


        //Client->Server 
        
        public void C2S_RequestConnect( string guid )
        {
            InvokeServerRpc("RecvRequestConnect", guid, NetManager.LocalClientId, MLAPI.Security.SecuritySendFlags.None);
        }
        [MLAPI.Messaging.ServerRPC(RequireOwnership = false)]
        private void RecvRequestConnect( string guid, ulong clientId )
        {


        }
    }
}
