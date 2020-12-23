using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BossRoom;

namespace BossRoom.Server
{
    /// <summary>
    /// Server logic plugin for the GameNetHub. Contains implementations for all GameNetHub's C2S RPCs. 
    /// </summary>
    public class ServerGNHLogic
    {
        private GameNetPortal m_Hub;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage. 
        private const int k_MaxConnectPayload = 1024;

        public ServerGNHLogic(GameNetPortal hub)
        {
            m_Hub = hub;
            m_Hub.NetManager.ConnectionApprovalCallback += this.ApprovalCheck;

            //The "BossRoom" server always advances to CharSelect immediately on start. Different games
            //may do this differently. 
            MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("CharSelect");
        }

        /// <summary>
        /// Initializes host mode on this client. Call this and then other clients should connect to us!
        /// </summary>
        /// <remarks>
        /// See notes in GNH_Client.StartClient about why this must be static. 
        /// </remarks>
        /// <param name="hub">The GameNetHub that is invoking us. </param>
        /// <param name="ipaddress">The IP address to connect to (currently IPV4 only).</param>
        /// <param name="port">The port to connect to. </param>
        public static void StartHost(GameNetPortal hub, string ipaddress, int port )
        {
            //FIXME: [GOMPS-34] move to UTP transport. 
            var transport = hub.NetworkingManagerGO.GetComponent<MLAPI.Transports.UNET.UnetTransport>();
            transport.ConnectAddress = ipaddress;
            transport.ServerListenPort = port;

            hub.NetManager.StartHost();
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalCallback" exposed by MLAPI.NetworkingManager, and is run every time a client connects to us. 
        /// See GNH_Client.StartClient for the complementary logic that runs when the client starts its connection. 
        /// </summary>
        /// <remarks>
        /// Since our game doesn't have to interact with some third party authentication service to validate the identity of the new connection, our ApprovalCheck
        /// method is simple, and runs synchronously, invoking "callback" to signal approval at the end of the method. MLAPI currently doesn't support the ability
        /// to send back more than a "true/false", which means we have to work a little harder to provide a useful error return to the client. To do that, we invoke a
        /// client RPC in the same channel that MLAPI uses for its connection callback. Since that channel ("MLAPI_INTERNAL") is both reliable and sequenced, we can be
        /// confident that our login result message will execute before any disconnect message. 
        /// </remarks>
        /// <param name="connectionData">binary data passed into StartClient. In our case this is the client's GUID, which is a unique identifier for their install of the game that persists across app restarts. </param>
        /// <param name="clientId">This is the clientId that MLAPI assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="callback">The delegate we must invoke to signal that the connection was approved or not. </param>
        private void ApprovalCheck( byte[] connectionData, ulong clientId, MLAPI.NetworkingManager.ConnectionApprovedDelegate callback )
        {
            if( connectionData.Length > k_MaxConnectPayload )
            {
                callback(false, 0, false, null, null );
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);

            string[] config_lines = payload.Split('\n');
            var payload_config = new Dictionary<string, string>();
            foreach( var line in config_lines )
            {
                //key-value pair. 
                if( line.Contains("=") )
                {
                    string[] kv = line.Split('=');
                    payload_config.Add(kv[0], kv[1]);
                }
                else if( line.Trim() != "" )
                {
                    //single token, with no value present. 
                    payload_config.Add(line, null);
                }
            }

            //TODO: GOMPS-78. We'll need to save our client guid so that we can handle reconnect. 
            Debug.Log("host ApprovalCheck: client guid was: " + payload_config["client_guid"]);

            
            //TODO: GOMPS-79 handle different error cases. 

            callback(false, 0, true, null, null);

            //FIXME_DMW: it is weird to do this after the callback, but the custom message won't be delivered if we call it beforehand.
            //This creates an "honor system" scenario where it is up to the client to politely leave on failure. Probably 
            //we should add a NetManager.DisconnectClient call directly below this line, when we are rejecting the connection. 
            m_Hub.S2C_ConnectResult(clientId, ConnectStatus.SUCCESS );
        }

    }
}

