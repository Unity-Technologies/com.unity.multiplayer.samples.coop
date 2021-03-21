using System.Collections.Generic;
using MLAPI.SceneManagement;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Represents a single player on the game server
    /// </summary>
    public struct PlayerData
    {
        public string playerName;  //name of the player
        public ulong clientID; //the identifying id of the client

        public PlayerData(string playerName, ulong clientId)
        {
            this.playerName = playerName;
            clientID = clientId;
        }
    }
    /// <summary>
    /// Server logic plugin for the GameNetHub. Contains implementations for all GameNetHub's C2S RPCs.
    /// </summary>
    public class ServerGameNetPortal : MonoBehaviour
    {
        GameNetPortal m_Portal;

        /// <summary>
        /// Maps a given client guid to the data for a given client player.
        /// </summary>
        Dictionary<string, PlayerData> m_ClientData;

        /// <summary>
        /// Map to allow us to cheaply map from guid to player data.
        /// </summary>
        Dictionary<ulong, string> m_ClientIDToGuid;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        const int k_MaxConnectPayload = 1024;

        /// <summary>
        /// Keeps a list of what clients are in what scenes.
        /// </summary>
        Dictionary<ulong, int> m_ClientSceneMap = new Dictionary<ulong, int>();

        /// <summary>
        /// The active server scene index.
        /// </summary>
        static int ServerScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        void Start()
        {
            m_Portal = GetComponent<GameNetPortal>();
            m_Portal.NetworkStarted += NetworkStart;
            // we add ApprovalCheck callback BEFORE NetworkStart to avoid spurious MLAPI warning:
            // "No ConnectionApproval callback defined. Connection approval will timeout"
            m_Portal.NetManager.ConnectionApprovalCallback += ApprovalCheck;
            m_Portal.NetManager.OnServerStarted += ServerStartedHandler;
            m_ClientData = new Dictionary<string, PlayerData>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();
        }

        void NetworkStart()
        {
            if (!m_Portal.NetManager.IsServer)
            {
                enabled = false;
            }
            else
            {
                //The "BossRoom" server always advances to CharSelect immediately on start. Different games
                //may do this differently.
                NetworkSceneManager.SwitchScene("CharSelect");

                m_Portal.NetManager.OnClientDisconnectCallback += clientId =>
                {
                    m_ClientSceneMap.Remove(clientId);
                };

                m_Portal.ClientSceneChanged += (clientId, sceneIndex) =>
                {
                    m_ClientSceneMap[clientId] = sceneIndex;
                };

                if( m_Portal.NetManager.IsHost)
                {
                    m_ClientSceneMap[m_Portal.NetManager.LocalClientId] = ServerScene;
                }
            }
        }

        public bool AreAllClientsInServerScene()
        {
            foreach( var kvp in m_ClientSceneMap )
            {
                if( kvp.Value != ServerScene ) { return false; }
            }

            return true;
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

                Debug.Log("No PlayerData of matching guid found");
            }
            else
            {
                Debug.Log("No client guid found mapped to the given client ID");
            }
            return null;
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalCallback" exposed by MLAPI.NetworkManager, and is run every time a client connects to us.
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
        void ApprovalCheck(byte[] connectionData, ulong clientId, MLAPI.NetworkManager.ConnectionApprovedDelegate callback)
        {
            if (connectionData.Length > k_MaxConnectPayload)
            {
                callback(false, 0, false, null, null);
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            int clientScene = connectionPayload.clientScene;

            m_ClientSceneMap[clientId] = clientScene;

            //TODO: GOMPS-78. We'll need to save our client guid so that we can handle reconnect.
            Debug.Log("host ApprovalCheck: client guid was: " + connectionPayload.clientGUID);

            //Populate our dictionaries with the playerData
            m_ClientIDToGuid[clientId] = connectionPayload.clientGUID;
            m_ClientData[connectionPayload.clientGUID] = new PlayerData(connectionPayload.playerName, clientId);

            //TODO: GOMPS-79 handle different error cases.

            callback(false, 0, true, null, null);

            //FIXME_DMW: it is weird to do this after the callback, but the custom message won't be delivered if we call it beforehand.
            //This creates an "honor system" scenario where it is up to the client to politely leave on failure. Probably
            //we should add a NetManager.DisconnectClient call directly below this line, when we are rejecting the connection.
            m_Portal.S2CConnectResult(clientId, ConnectStatus.Success);
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        void ServerStartedHandler()
        {
            m_ClientData.Add("host_guid", new PlayerData(m_Portal.PlayerName, m_Portal.NetManager.LocalClientId));
            m_ClientIDToGuid.Add(m_Portal.NetManager.LocalClientId, "host_guid");
        }
    }
}
