using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BossRoom;

namespace BossRoomServer
{
    public class GNH_Server
    {
        private GameNetHub m_hub;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage. 
        private const int MAX_CONNECT_PAYLOAD = 1024;

        public GNH_Server(GameNetHub hub)
        {
            m_hub = hub;
            m_hub.NetManager.ConnectionApprovalCallback += this.ApprovalCheck;
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
            if( connectionData.Length > MAX_CONNECT_PAYLOAD )
            {
                callback(false, 0, false, null, null );
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);

            string[] config_lines = payload.Split('\n');
            Dictionary<string, string> payload_config = new Dictionary<string, string>();
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

            //TODO: save off the player's Guid.
            Debug.Log("host ApprovalCheck: client payload was: " + payload);
            Debug.Log("host ApprovalCheck: client guid was: " + payload_config["client_guid"]);

            //TODO: handle different cases based on gamestate (e.g. GameState.PLAYING would cause a reject if this isn't a reconnect case). 

            //TODO: add corresponding NetworkHide, just so that we don't endlessly leak clientIds into our observer's list. The
            //GameNetHub should be observable by everybody all the time. 
            m_hub.GetComponent<MLAPI.NetworkedObject>().NetworkShow(clientId);

            m_hub.S2C_ConnectResult(clientId, ConnectStatus.SUCCESS, BossRoomState.CHARSELECT );

            callback(false, 0, true, null, null);
        }

    }
}

