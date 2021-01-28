using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Represents a single player on the game server
    /// </summary>
    public struct PlayerData
    {
        public string m_PlayerName;  //name of the player
        public ulong m_ClientID; //the identifying id of the client

        public PlayerData(string playerName, ulong clientId)
        {
            m_PlayerName = playerName;
            m_ClientID = clientId;
        }
    }
    /// <summary>
    /// Server logic plugin for the GameNetHub. Contains implementations for all GameNetHub's C2S RPCs. 
    /// </summary>
    public class ServerGameNetPortal : MonoBehaviour
    {
        private GameNetPortal m_Hub;

        /// <summary>
        /// Maps a given client guid to the data for a given client player.
        /// </summary>
        private Dictionary<string, PlayerData> m_ClientData;

        /// <summary>
        /// Map to allow us to cheaply map from guid to player data.
        /// </summary>
        private Dictionary<ulong, string> m_ClientIDToGuid;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage. 
        private const int k_MaxConnectPayload = 1024;

        public void Start()
        {
            m_Hub = GetComponent<GameNetPortal>();
            m_Hub.networkStartEvent += NetworkStart;
            // we add ApprovalCheck callback BEFORE NetworkStart to avoid spurious MLAPI warning:
            // "No ConnectionApproval callback defined. Connection approval will timeout"
            m_Hub.NetManager.ConnectionApprovalCallback += ApprovalCheck;
            m_Hub.NetManager.OnServerStarted += ServerStartedHandler;
            m_ClientData = new Dictionary<string, PlayerData>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();
        }

        public void NetworkStart()
        {
            if (!m_Hub.NetManager.IsServer)
            {
                enabled = false;
            }
            else
            {
                //The "BossRoom" server always advances to CharSelect immediately on start. Different games
                //may do this differently. 
                MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("CharSelect");
            }


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"> guid of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public PlayerData? GetPlayerData(ulong clientId)
        {
            //First see if we have a guid matching the clientID given.

            if (m_ClientIDToGuid.TryGetValue(clientId, out string clientguid))
            {
                if (m_ClientData.TryGetValue(clientguid, out PlayerData data))
                {
                    return data;
                }
                else
                {
                    Debug.Log("No PlayerData of matching guid found");
                }
            }
            else
            {
                Debug.Log("No client guid found mapped to the given client ID");
            }
            return null;
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
        private void ApprovalCheck(byte[] connectionData, ulong clientId, MLAPI.NetworkingManager.ConnectionApprovedDelegate callback)
        {
            if (connectionData.Length > k_MaxConnectPayload)
            {
                callback(false, 0, false, null, null);
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);

            string[] config_lines = payload.Split('\n');
            var payload_config = new Dictionary<string, string>();
            foreach (var line in config_lines)
            {
                //key-value pair. 
                if (line.Contains("="))
                {
                    string[] kv = line.Split('=');
                    payload_config.Add(kv[0], kv[1]);
                }
                else if (line.Trim() != "")
                {
                    //single token, with no value present. 
                    payload_config.Add(line, null);
                }
            }

            //TODO: GOMPS-78. We'll need to save our client guid so that we can handle reconnect. 
            Debug.Log("host ApprovalCheck: client guid was: " + payload_config["client_guid"]);

            //Populate our dictionaries with the playerData
            m_ClientIDToGuid.Add(clientId, payload_config["client_guid"]);

            m_ClientData.Add(payload_config["client_guid"], new PlayerData(payload_config["player_name"], clientId));


            //TODO: GOMPS-79 handle different error cases. 

            callback(false, 0, true, null, null);

            //FIXME_DMW: it is weird to do this after the callback, but the custom message won't be delivered if we call it beforehand.
            //This creates an "honor system" scenario where it is up to the client to politely leave on failure. Probably 
            //we should add a NetManager.DisconnectClient call directly below this line, when we are rejecting the connection. 
            m_Hub.S2C_ConnectResult(clientId, ConnectStatus.SUCCESS);
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        private void ServerStartedHandler()
        {
            m_ClientData.Add("host_guid", new PlayerData(m_Hub.PlayerName, m_Hub.NetManager.LocalClientId));
            m_ClientIDToGuid.Add(m_Hub.NetManager.LocalClientId, "host_guid");
        }

    }
}

