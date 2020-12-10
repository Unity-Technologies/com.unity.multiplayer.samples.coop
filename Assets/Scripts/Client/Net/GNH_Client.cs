using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BossRoom;

namespace BossRoomClient
{
    /// <summary>
    /// Client logic for the GameNetHub. Contains implementations for all of GameNetHub's S2C RPCs. 
    /// </summary>
    public class GNH_Client
    {
        private GameNetHub m_hub;

        public GNH_Client(GameNetHub hub)
        {
            m_hub = hub;
        }

        public void RecvConnectFinished( ConnectStatus status, BossRoomState targetState )
        {
            //on success, there is nothing to do (the MLAPI scene management system will take us to the next scene). 
            //on failure, we must raise an event so that the UI layer can display something. 
            Debug.Log("RecvConnectFinished Got status: " + status);
            m_hub.GetComponent<BossRoomStateManager>().ChangeState(targetState, null);
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
        /// <param name="ipaddress">the IP address of the host to connect to.</param>
        /// <param name="port">The port of the host to connect to. </param>
        public static void StartClient(GameNetHub hub, string ipaddress, int port)
        {
            string client_guid = GetOrCreateGuid();
            string payload = $"client_guid={client_guid}\n"; //minimal format where key=value pairs are separated by newlines. 

            Debug.Log("client send payload: " + payload); //dmw_debug: remove. 

            byte[] payload_bytes = System.Text.Encoding.UTF8.GetBytes(payload);

            //fixme: this code is not portable, and will break depending on the transport used. Unfortunately different transports call these
            //fields different things, so we might need a big switch-case to handle more than one transport. Or we can update this once
            //if we have decisively normalized on UTP transport. 
            var transport = hub.NetworkingManagerGO.GetComponent<MLAPI.Transports.UNET.UnetTransport>();
            transport.ConnectAddress = ipaddress;
            transport.ConnectPort = port;

            hub.NetManager.NetworkConfig.ConnectionData = payload_bytes;

            //and...we're off! MLAPI will establish a socket connection to the host. 
            //  If the socket connection fails, we'll hear back by [???] (fixme: where do we get transport-layer failures?)
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported. 
            hub.NetManager.StartClient();
        }

    }
}
