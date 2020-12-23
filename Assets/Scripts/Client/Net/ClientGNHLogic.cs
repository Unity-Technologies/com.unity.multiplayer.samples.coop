using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BossRoom;

namespace BossRoom.Client
{
    /// <summary>
    /// Client logic for the GameNetHub. Contains implementations for all of GameNetHub's S2C RPCs. 
    /// </summary>
    public class ClientGNHLogic
    {
        private GameNetPortal m_hub;

        public ClientGNHLogic(GameNetPortal hub)
        {
            m_hub = hub;
        }

        public void RecvConnectFinished( ConnectStatus status )
        {
            //on success, there is nothing to do (the MLAPI scene management system will take us to the next scene). 
            //on failure, we must raise an event so that the UI layer can display something. 
            Debug.Log("RecvConnectFinished Got status: " + status);
            //m_hub.GetComponent<BossRoomStateManager>().ChangeState(targetState, null);
        }


        /// <summary>
        /// Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it. 
        /// </summary>
        /// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
        private static string GetOrCreateGuid()
        {
            if( PlayerPrefs.HasKey("client_guid"))
            {
                return PlayerPrefs.GetString("client_guid");
            }

            System.Guid g = System.Guid.NewGuid();
            string guid_string = g.ToString();

            PlayerPrefs.SetString("client_guid", guid_string);
            return guid_string;
        }

        /// <summary>
        /// Wraps the invocation of NetworkingManager.StartClient, including our GUID as the payload. 
        /// </summary>
        /// <remarks>
        /// This method must be static because, when it is invoked, the client still doesn't know it's a client yet, and in particular, GameNetHub hasn't
        /// yet initialized its client and server GNHLogic objects yet (which it does in NetworkStart, based on the role that the current player is performing). 
        /// </remarks>
        /// <param name="ipaddress">the IP address of the host to connect to. (currently IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public static void StartClient(GameNetPortal hub, string ipaddress, int port)
        {
            string client_guid = GetOrCreateGuid();
            string payload = $"client_guid={client_guid}\n"; //minimal format where key=value pairs are separated by newlines. 

            byte[] payload_bytes = System.Text.Encoding.UTF8.GetBytes(payload);

            //FIXME: [GOMPS-34] move to UTP transport. 
            var transport = hub.NetworkingManagerGO.GetComponent<MLAPI.Transports.UNET.UnetTransport>();
            transport.ConnectAddress = ipaddress;
            transport.ConnectPort = port;

            hub.NetManager.NetworkConfig.ConnectionData = payload_bytes;

            //and...we're off! MLAPI will establish a socket connection to the host. 
            //  If the socket connection fails, we'll hear back by [???] (FIXME: GOMPS-79, need to handle transport layer failures too).
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported. 
            hub.NetManager.StartClient();
        }

    }
}
