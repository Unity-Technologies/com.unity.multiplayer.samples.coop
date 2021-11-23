using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public interface ISessionPlayerData
    {
        bool IsConnected { get; set; }
        ulong ClientID { get; set; }
        void Reinitialize();
    }

    public class SessionManager<T> : MonoBehaviour where T : struct, ISessionPlayerData
    {
        const string k_HostGUID = "host_guid";

        [SerializeField]
        NetworkManager m_NetworkManager;

        public static SessionManager<T> Instance { get; private set; }

        /// <summary>
        /// Maps a given client guid to the data for a given client player.
        /// </summary>
        private Dictionary<string, T> m_ClientData;

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
            if (m_NetworkManager)
            {
                m_NetworkManager.OnServerStarted += ServerStartedHandler;
            }

            m_ClientData = new Dictionary<string, T>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();

            DontDestroyOnLoad(this);
        }

        void OnDestroy()
        {
            if (m_NetworkManager)
            {
                m_NetworkManager.OnServerStarted -= ServerStartedHandler;
            }
        }

        public void AddHostData(T sessionPlayerData)
        {
            m_ClientData.Add(k_HostGUID, sessionPlayerData);
            m_ClientIDToGuid.Add(m_NetworkManager.LocalClientId, k_HostGUID);
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            if (m_ClientIDToGuid.TryGetValue(clientId, out var guid))
            {
                if (GetPlayerData(guid)?.ClientID == clientId)
                {
                    var clientData = m_ClientData[guid];
                    clientData.IsConnected = false;
                    m_ClientData[guid] = clientData;
                }
            }

            if (clientId == m_NetworkManager.LocalClientId)
            {
                //the SessionManager may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }
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
        /// <param name="sessionPlayerData">The player's initial data</param>
        /// <returns></returns>
        public ConnectStatus OnClientApprovalCheck(ulong clientId, string clientGUID, T sessionPlayerData)
        {
            ConnectStatus gameReturnStatus = ConnectStatus.Success;

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
                        while (m_ClientData.ContainsKey(clientGUID) && m_ClientData[clientGUID].IsConnected) clientGUID += "_Secondary";

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

        /// <summary>
        ///
        /// </summary>
        /// <param name="clientId"> id of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public T? GetPlayerData(ulong clientId)
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
        public T? GetPlayerData(string clientGUID)
        {
            if (m_ClientData.TryGetValue(clientGUID, out T data))
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
        public void SetPlayerData(ulong clientId, T sessionPlayerData)
        {
            if (m_ClientIDToGuid.TryGetValue(clientId, out string clientGUID))
            {
                m_ClientData[clientGUID] = sessionPlayerData;
            }
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        private void ServerStartedHandler()
        {
            if (!m_NetworkManager.IsServer)
            {
                enabled = false;
            }
            else
            {
                //O__O if adding any event registrations here, please add an unregistration in OnClientDisconnect.
                m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
            }
        }

        /// <summary>
        /// Reinitializes session data from connected players, and clears data from disconnected players, so that if they reconnect in the next game, they will be treated as new players
        /// </summary>
        public void OnGameEnded()
        {
            List<ulong> idsToClear = new List<ulong>();
            List<ulong> connectedClientIds = new List<ulong>(m_NetworkManager.ConnectedClientsIds);
            foreach (var id in m_ClientIDToGuid.Keys)
            {
                if (!connectedClientIds.Contains(id))
                {
                    idsToClear.Add(id);
                }
                else
                {
                    string guid = m_ClientIDToGuid[id];
                    T sessionPlayerData = m_ClientData[guid];
                    sessionPlayerData.Reinitialize();
                    m_ClientData[guid] = sessionPlayerData;
                }
            }

            foreach (var id in idsToClear)
            {
                string guid = m_ClientIDToGuid[id];
                if (GetPlayerData(guid)?.ClientID == id)
                {
                    m_ClientData.Remove(guid);
                }

                m_ClientIDToGuid.Remove(id);
            }
        }
    }
}
