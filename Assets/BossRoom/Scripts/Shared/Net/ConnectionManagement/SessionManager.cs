using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct SessionPlayerData
    {
        public ulong ClientID;
        public string PlayerName;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public NetworkGuid AvatarNetworkGuid;
        public int CurrentHitPoints;
        public bool IsConnected;
        public bool HasCharacterSpawned;

        public SessionPlayerData(ulong clientID, string name, NetworkGuid avatarNetworkGuid, int currentHitPoints = 0, bool isConnected = false, bool hasCharacterSpawned = false)
        {
            ClientID = clientID;
            PlayerName = name;
            PlayerPosition = Vector3.zero;
            PlayerRotation = Quaternion.identity;
            AvatarNetworkGuid = avatarNetworkGuid;
            CurrentHitPoints = currentHitPoints;
            IsConnected = isConnected;
            HasCharacterSpawned = hasCharacterSpawned;
        }
    }

    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [SerializeField]
        private GameNetPortal m_Portal;

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;

        /// <summary>
        /// Maps a given client guid to the data for a given client player.
        /// </summary>
        private Dictionary<string, SessionPlayerData> m_ClientData;

        /// <summary>
        /// Map to allow us to cheaply map from guid to player data.
        /// </summary>
        private Dictionary<ulong, string> m_ClientIDToGuid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        void Start()
        {
            if (m_Portal == null) return;

            m_Portal.NetManager.OnServerStarted += ServerStartedHandler;
            m_ClientData = new Dictionary<string, SessionPlayerData>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();

            DontDestroyOnLoad(this);
        }

        void OnDestroy()
        {
            if (m_Portal != null)
            {
                if (m_Portal.NetManager != null)
                {
                    m_Portal.NetManager.OnServerStarted -= ServerStartedHandler;
                }
            }
        }

        public void AddHostData()
        {
            m_ClientData.Add("host_guid", new SessionPlayerData(m_Portal.NetManager.LocalClientId, m_Portal.PlayerName, m_AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));
            m_ClientIDToGuid.Add(m_Portal.NetManager.LocalClientId, "host_guid");
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            if (m_ClientIDToGuid.TryGetValue(clientId, out var guid))
            {
                if (m_ClientData[guid].ClientID == clientId)
                {
                    var sessionPlayerData = m_ClientData[guid];
                    sessionPlayerData.IsConnected = false;
                    m_ClientData[guid] = sessionPlayerData;
                    // Wait a few seconds before removing the clientId-GUID mapping to allow for final updates to SessionPlayerData.
                    StartCoroutine(WaitToRemoveClientId(clientId));
                }
            }

            if (clientId == m_Portal.NetManager.LocalClientId)
            {
                //the SessionManager may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                m_Portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        private IEnumerator WaitToRemoveClientId(ulong clientId)
        {
            yield return new WaitForSeconds(5.0f);
            m_ClientIDToGuid.Remove(clientId);
        }


        /// <summary>
        /// Handles the flow when a user has requested a disconnect via UI (which can be invoked on the Host, and thus must be
        /// handled in server code).
        /// </summary>
        public void OnUserDisconnectRequest()
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
        /// Handles the flow when a user is connecting by testing for duplicate logins and populating the session's data.
        /// Invoked during the approval check and returns the connection status.
        /// </summary>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="clientGUID">This is the clientGUID that is unique to this client and persists accross multiple logins from the same client</param>
        /// <param name="playerName">The player's name</param>
        /// <returns></returns>
        public ConnectStatus OnClientApprovalCheck(ulong clientId, string clientGUID, string playerName)
        {
            ConnectStatus gameReturnStatus = ConnectStatus.Success;
            SessionPlayerData sessionPlayerData = new SessionPlayerData(clientId, playerName, m_AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true, false);
            //Test for Duplicate Login.
            if (m_ClientData.ContainsKey(clientGUID))
            {
                bool isReconnecting = false;
                // If another client is connected with the same clientGUID
                if (m_ClientData[clientGUID].IsConnected)
                {
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log($"Client GUID {clientGUID} already exists. Because this is a debug build, we will still accept the connection");
                        // If debug build, accept connection and manually update clientGUID until we get one that either is not connected or that does not already exist
                        while (m_ClientData.ContainsKey(clientGUID) && m_ClientData[clientGUID].IsConnected) {clientGUID += "_Secondary"; }

                        if (m_ClientData.ContainsKey(clientGUID) && !m_ClientData[clientGUID].IsConnected)
                        {
                            // In this specific case, if the clients with the same GUID reconnect in a different order than when they originally connected,
                            // they will swap characters, since their GUIDs are manually modified here at runtime.
                            isReconnecting = true;
                        }
                    }
                    else
                    {
                        gameReturnStatus = ConnectStatus.LoggedInAgain;
                    }
                }
                else
                {
                    isReconnecting = true;
                }

                // Reconnecting. Give data from old player to new player
                if (isReconnecting)
                {
                    // Update player session data
                    sessionPlayerData = m_ClientData[clientGUID];
                    sessionPlayerData.ClientID = clientId;
                    sessionPlayerData.IsConnected = true;
                }

            }

            //Populate our dictionaries with the SessionPlayerData
            if (gameReturnStatus == ConnectStatus.Success)
            {
                m_ClientIDToGuid[clientId] = clientGUID;
                m_ClientData[clientGUID] = sessionPlayerData;
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

            if (m_ClientIDToGuid.TryGetValue(clientId, out string clientGUID))
            {
                return GetPlayerData(clientGUID);
            }

            Debug.Log("No client guid found mapped to the given client ID");
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

            Debug.Log("No PlayerData of matching guid found");
            return null;
        }

        /// <summary>
        /// Updates player data
        /// </summary>
        /// <param name="clientId"> id of the client whose data will be updated </param>
        /// <param name="sessionPlayerData"> new data to overwrite the old </param>
        public void SetPlayerData(ulong clientId, SessionPlayerData sessionPlayerData)
        {
            if (m_ClientIDToGuid.TryGetValue(clientId, out string clientGUID))
            {
                m_ClientData[clientGUID] = sessionPlayerData;
            }
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
        /// Clears data from disconnected players, so that if they reconnect in the next game, they will be treated as new players
        /// </summary>
        public void OnGameEnded()
        {
            List<ulong> idsToClear = new List<ulong>();
            List<ulong> connectedClientIds = new List<ulong>(m_Portal.NetManager.ConnectedClientsIds);
            foreach (var id in m_ClientIDToGuid.Keys)
            {
                if (!connectedClientIds.Contains(id))
                {
                    idsToClear.Add(id);
                }
            }

            foreach (var id in idsToClear)
            {
                m_ClientData.Remove(m_ClientIDToGuid[id]);
                m_ClientIDToGuid.Remove(id);
            }
        }
    }
}
