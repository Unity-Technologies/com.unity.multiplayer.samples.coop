using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
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

            //m_Portal.NetworkReadied += OnNetworkReady;

            // we add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious NGO warning:
            // "No ConnectionApproval callback defined. Connection approval will timeout"
            m_Portal.NetManager.OnServerStarted += ServerStartedHandler;
            m_ClientData = new Dictionary<string, SessionPlayerData>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();

            DontDestroyOnLoad(this);
        }

        void OnDestroy()
        {
            if (m_Portal != null)
            {
                //m_Portal.NetworkReadied -= OnNetworkReady;

                if (m_Portal.NetManager != null)
                {
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
                //m_Portal.UserDisconnectRequested += OnUserDisconnectRequest;
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
                    /*
                    var character = PlayerServerCharacter.GetPlayerServerCharacters().Find(
                        player => player.OwnerClientId == clientId);
                    m_ClientData[m_ClientIDToGuid[clientId]] = new SessionPlayerData(clientId, m_ClientIDToGuid[clientId], m_Portal.PlayerName, character.transform.position, character.transform.rotation.eulerAngles, false);
                    */
                    m_ClientData.Remove(guid);
                }
            }

            if (clientId == m_Portal.NetManager.LocalClientId)
            {
                //the ServerGameNetPortal may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                //m_Portal.UserDisconnectRequested -= OnUserDisconnectRequest;
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

        public bool IsServerFull()
        {
            return m_ClientData.Count >= CharSelectData.k_MaxLobbyPlayers;
        }

        public ConnectStatus OnClientConnected(ulong clientId, string clientGUID, string playerName)
        {
            ConnectStatus gameReturnStatus = ConnectStatus.Success;
            //Test for Duplicate Login.
            if (m_ClientData.ContainsKey(clientGUID))
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"Client GUID {clientGUID} already exists. Because this is a debug build, we will still accept the connection");
                    while (m_ClientData.ContainsKey(clientGUID)) { clientGUID += "_Secondary"; }
                }
                else
                {
                    gameReturnStatus = ConnectStatus.LoggedInAgain;
                }
            }

            //Populate our dictionaries with the SessionPlayerData
            if (gameReturnStatus == ConnectStatus.Success)
            {
                m_ClientIDToGuid[clientId] = clientGUID;
                m_ClientData[clientGUID] = new SessionPlayerData(clientId, clientGUID, playerName, m_InitialPositionRotation, m_InitialPositionRotation, true);
            }

            return gameReturnStatus;
        }


        public string GetPlayerGUID(ulong clientID)
        {
            return m_ClientIDToGuid[clientID];
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="clientId"> id of the client whose data is requested</param>
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
        ///
        /// </summary>
        /// <param name="clientGUID"> guid of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public SessionPlayerData? GetPlayerData(string clientGUID)
        {
            //First see if we have a guid matching the clientID given.


            if (m_ClientData.TryGetValue(clientGUID, out SessionPlayerData data))
            {
                return data;
            }
            else
            {
                Debug.Log("No PlayerData of matching guid found");
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
