using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;
using MLAPI.SceneManagement;

namespace BossRoom.Server
{
    public struct SessionPlayerData
    {
        public ulong ClientID;
        public string ClientGUID;
        public string PlayerName;
        public Vector3 PlayerPosition;
        public Vector3 PlayerRotation;
        public bool IsConnected;

        public SessionPlayerData(ulong clientID, string clientGUID, string name, Vector3 position, Vector3 rotation, bool isConnected = false)
        {
            ClientID = clientID;
            ClientGUID = clientGUID;
            PlayerName = name;
            PlayerPosition = position;
            PlayerRotation = rotation;
            IsConnected = isConnected;
        }
    }

    public class ServerSessionManager : MonoBehaviour
    {
        private static ServerSessionManager _instance;

        public static ServerSessionManager Instance
        {
            get { return _instance; }
        }

        public GameNetPortal m_Portal;

        /// <summary>
        /// Maps a given client guid to the data for a given client player.
        /// </summary>
        private Dictionary<string, SessionPlayerData> m_ClientData;

        /// <summary>
        /// Map to allow us to cheaply map from guid to player data.
        /// </summary>
        private Dictionary<ulong, string> m_ClientIDToGuid;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        private const int k_MaxConnectPayload = 1024;

        private Vector3 m_InitialPositionRotation = Vector3.zero;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        void Start()
        {
            if (m_Portal == null) return;

            m_Portal.NetworkReadied += OnNetworkReady;

            // we add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious MLAPI warning:
            // "No ConnectionApproval callback defined. Connection approval will timeout"
            m_Portal.NetManager.ConnectionApprovalCallback += ApprovalCheck;
            m_Portal.NetManager.OnServerStarted += ServerStartedHandler;
            m_ClientData = new Dictionary<string, SessionPlayerData>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();

            DontDestroyOnLoad(this);
        }

        void OnDestroy()
        {
            if (m_Portal != null)
            {
                m_Portal.NetworkReadied -= OnNetworkReady;

                if (m_Portal.NetManager != null)
                {
                    m_Portal.NetManager.ConnectionApprovalCallback -= ApprovalCheck;
                    m_Portal.NetManager.OnServerStarted -= ServerStartedHandler;
                }
            }
        }

        private void OnNetworkReady()
        {
            if (!m_Portal.NetManager.IsServer)
            {
                enabled = false;
            }
            else
            {
                //O__O if adding any event registrations here, please add an unregistration in OnClientDisconnect.
                m_Portal.UserDisconnectRequested += OnUserDisconnectRequest;
                m_Portal.NetManager.OnClientDisconnectCallback += OnClientDisconnect;
            }
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            if (m_ClientIDToGuid.TryGetValue(clientId, out var guid))
            {
                m_ClientIDToGuid.Remove(clientId);

                if (m_ClientData[guid].ClientID == clientId)
                {
                    //be careful to only remove the ClientData if it is associated with THIS clientId; in a case where a new connection
                    //for the same GUID kicks the old connection, this could get complicated. In a game that fully supported the reconnect flow,
                    //we would NOT remove ClientData here, but instead time it out after a certain period, since the whole point of it is
                    //to remember client information on a per-guid basis after the connection has been lost.
                    var character = PlayerServerCharacter.GetPlayerServerCharacters().Find(
                        player => player.OwnerClientId == clientId);
                    m_ClientData[m_ClientIDToGuid[clientId]] = new SessionPlayerData(clientId, m_ClientIDToGuid[clientId], m_Portal.PlayerName, character.transform.position, character.transform.rotation.eulerAngles, false);
                }
            }

            if (clientId == m_Portal.NetManager.LocalClientId)
            {
                //the ServerGameNetPortal may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                m_Portal.UserDisconnectRequested -= OnUserDisconnectRequest;
                m_Portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }


        /// <summary>
        /// Handles the flow when a user has requested a disconnect via UI (which can be invoked on the Host, and thus must be
        /// handled in server code).
        /// </summary>
        private void OnUserDisconnectRequest()
        {
            Clear();
        }

        private void Clear()
        {
            //resets all our runtime state.
            m_ClientData.Clear();
            m_ClientIDToGuid.Clear();
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
        private void ApprovalCheck(byte[] connectionData, ulong clientId, MLAPI.NetworkManager.ConnectionApprovedDelegate callback)
        {
            if (connectionData.Length > k_MaxConnectPayload)
            {
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            if (connectionPayload == null)
            {
                return;
            }
            
            Debug.Log("Host ApprovalCheck: connecting client GUID: " + connectionPayload.clientGUID);

            ConnectStatus gameReturnStatus = ConnectStatus.Success;

            //Test for Duplicate Login.
            if (m_ClientData.ContainsKey(connectionPayload.clientGUID))
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"Client GUID {connectionPayload.clientGUID} already exists. Because this is a debug build, we will still accept the connection");
                    while (m_ClientData.ContainsKey(connectionPayload.clientGUID)) { connectionPayload.clientGUID += "_Secondary"; }
                }
                else
                {
                    gameReturnStatus = ConnectStatus.LoggedInAgain;
                }
            }

            //Test for over-capacity Login.
            if (m_ClientData.Count >= CharSelectData.k_MaxLobbyPlayers)
            {
                gameReturnStatus = ConnectStatus.ServerFull;
            }

            //Populate our dictionaries with the SessionPlayerData
            if (gameReturnStatus == ConnectStatus.Success)
            {
                m_ClientIDToGuid[clientId] = connectionPayload.clientGUID;
                m_ClientData[connectionPayload.clientGUID] = new SessionPlayerData(clientId, connectionPayload.clientGUID, connectionPayload.playerName, m_InitialPositionRotation, m_InitialPositionRotation, true);
            }
        }

        public string GetPlayerGUID(ulong clientID)
        {
            return m_ClientIDToGuid[clientID];
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="clientId"> guid of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public SessionPlayerData? GetPlayerData(ulong clientId)
        {
            //First see if we have a guid matching the clientID given.

            if (m_ClientIDToGuid.TryGetValue(clientId, out string clientguid))
            {
                if (m_ClientData.TryGetValue(clientguid, out SessionPlayerData data))
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
        /// Convenience method to get player name from player data
        /// Returns name in data or default name using playerNum
        /// </summary>
        public string GetPlayerName(ulong clientId, int playerNum)
        {
            var playerData = GetPlayerData(clientId);
            return (playerData != null) ? playerData.Value.PlayerName : ("Player" + playerNum);
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        private void ServerStartedHandler()
        {
            m_ClientData.Add("host_guid", new SessionPlayerData(m_Portal.NetManager.LocalClientId, "host_guid", m_Portal.PlayerName, m_InitialPositionRotation, m_InitialPositionRotation, true));
            m_ClientIDToGuid.Add(m_Portal.NetManager.LocalClientId, "host_guid");
        }

        public void UpdatePlayer(ulong clientId, string name, Vector3 position, Vector3 rotation)
        {
            var guid = m_ClientIDToGuid[clientId];
            m_ClientData[guid] = new SessionPlayerData(clientId, guid, name, position, rotation, true);
        }
    }
}