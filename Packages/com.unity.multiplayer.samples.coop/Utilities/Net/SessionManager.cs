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

    /// <summary>
    /// This class uses a GUID to bind a player to a session. Once that player connects to a host, the host associates
    /// the current ClientID to the player's session GUID. If the player disconnects and reconnects to the same host,
    /// the session is preserved.
    /// </summary>
    /// <remarks>
    /// Using a client-generated GUID and sending it directly could be problematic, as a malicious user could intercept
    /// it and reuse it to impersonate the original user. We are currently investigating this to offer a solution that
    /// handles security better.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class SessionManager<T> where T : struct, ISessionPlayerData
    {
        const string k_HostGUID = "host_guid";

        NetworkManager m_NetworkManager;

        protected SessionManager()
        {
            m_NetworkManager = NetworkManager.Singleton;
            if (m_NetworkManager)
            {
                m_NetworkManager.OnServerStarted += ServerStartedHandler;
            }

            m_ClientData = new Dictionary<string, T>();
            m_ClientIDToPlayerId = new Dictionary<ulong, string>();
        }

        ~SessionManager()
        {
            if (m_NetworkManager)
            {
                m_NetworkManager.OnServerStarted -= ServerStartedHandler;
            }
        }

        public static SessionManager<T> Instance => s_Instance ??= new SessionManager<T>();

        private static SessionManager<T> s_Instance;

        /// <summary>
        /// Maps a given client player id to the data for a given client player.
        /// </summary>
        private Dictionary<string, T> m_ClientData;

        /// <summary>
        /// Map to allow us to cheaply map from player id to player data.
        /// </summary>
        private Dictionary<ulong, string> m_ClientIDToPlayerId;

        public void AddHostData(T sessionPlayerData)
        {
            if (sessionPlayerData.ClientID == m_NetworkManager.ServerClientId)
            {
                m_ClientData.Add(k_HostGUID, sessionPlayerData);
                m_ClientIDToPlayerId.Add(sessionPlayerData.ClientID, k_HostGUID);
            }
            else
            {
                Debug.LogError($"Invalid ClientId for host. Got {sessionPlayerData.ClientID}, but should have gotten {m_NetworkManager.ServerClientId}.");
            }
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var playerId))
            {
                if (GetPlayerData(playerId)?.ClientID == clientId)
                {
                    var clientData = m_ClientData[playerId];
                    clientData.IsConnected = false;
                    m_ClientData[playerId] = clientData;
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
            m_ClientIDToPlayerId.Clear();
        }

        /// <summary>
        /// Adds a connecting player's session data if it is a new connection, or updates their session data in case of a reconnection. If the connection is not valid, simply returns false.
        /// </summary>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="playerId">This is the playerId that is unique to this client and persists accross multiple logins from the same client</param>
        /// <param name="sessionPlayerData">The player's initial data</param>
        /// <returns>True if the player connection is valid (i.e. not a duplicate connection)</returns>
        public bool SetupConnectingPlayerSessionData(ulong clientId, string playerId, T sessionPlayerData)
        {
            bool success = true;

            //Test for Duplicate Login.
            if (m_ClientData.ContainsKey(playerId))
            {
                bool isReconnecting = false;

                // If another client is connected with the same playerId
                if (m_ClientData[playerId].IsConnected)
                {
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log($"Client PlayerId {playerId} already exists. Because this is a debug build, we will still accept the connection");

                        // If debug build, accept connection and manually update playerId until we get one that either is not connected or that does not already exist
                        while (m_ClientData.ContainsKey(playerId) && m_ClientData[playerId].IsConnected) playerId += "_Secondary";

                        if (m_ClientData.ContainsKey(playerId) && !m_ClientData[playerId].IsConnected)
                        {
                            // In this specific case, if the clients with the same PlayerId reconnect in a different order than when they originally connected,
                            // they will swap characters, since their PlayerIds are manually modified here at runtime.
                            isReconnecting = true;
                        }
                    }
                    else
                    {
                        success = false;
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
                    sessionPlayerData = m_ClientData[playerId];
                    sessionPlayerData.ClientID = clientId;
                    sessionPlayerData.IsConnected = true;
                }
            }

            //Populate our dictionaries with the SessionPlayerData
            if (success)
            {
                m_ClientIDToPlayerId[clientId] = playerId;
                m_ClientData[playerId] = sessionPlayerData;
            }

            return success;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="clientId"> id of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public T? GetPlayerData(ulong clientId)
        {
            //First see if we have a playerId matching the clientID given.

            if (m_ClientIDToPlayerId.TryGetValue(clientId, out string playerId))
            {
                return GetPlayerData(playerId);
            }

            Debug.LogError($"No client player ID found mapped to the given client ID: {clientId}");
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="playerId"> Player ID of the client whose data is requested</param>
        /// <returns>Player data struct matching the given ID</returns>
        public T? GetPlayerData(string playerId)
        {
            if (m_ClientData.TryGetValue(playerId, out T data))
            {
                return data;
            }

            Debug.LogError($"No PlayerData of matching player ID found: {playerId}");
            return null;
        }

        /// <summary>
        /// Updates player data
        /// </summary>
        /// <param name="clientId"> id of the client whose data will be updated </param>
        /// <param name="sessionPlayerData"> new data to overwrite the old </param>
        public void SetPlayerData(ulong clientId, T sessionPlayerData)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out string playerId))
            {
                m_ClientData[playerId] = sessionPlayerData;
            }
            else
            {
                Debug.LogError($"No client player ID found mapped to the given client ID: {clientId}");
            }
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        private void ServerStartedHandler()
        {
            if (m_NetworkManager.IsServer)
            {
                //O__O if adding any event registrations here, please add an unregistration in OnClientDisconnect.
                m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
            }
        }

        public void OnSessionStarted()
        {
            ClearDisconnectedPlayersData();
        }

        /// <summary>
        /// Reinitializes session data from connected players, and clears data from disconnected players, so that if they reconnect in the next game, they will be treated as new players
        /// </summary>
        public void OnSessionEnded()
        {
            ClearDisconnectedPlayersData();
            List<ulong> connectedClientIds = new List<ulong>(m_NetworkManager.ConnectedClientsIds);
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                if (connectedClientIds.Contains(id))
                {
                    string playerId = m_ClientIDToPlayerId[id];
                    T sessionPlayerData = m_ClientData[playerId];
                    sessionPlayerData.Reinitialize();
                    m_ClientData[playerId] = sessionPlayerData;
                }
            }
        }

        void ClearDisconnectedPlayersData()
        {
            List<ulong> idsToClear = new List<ulong>();
            List<ulong> connectedClientIds = new List<ulong>(m_NetworkManager.ConnectedClientsIds);
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                if (!connectedClientIds.Contains(id))
                {
                    idsToClear.Add(id);
                }
                else
                {
                    string playerId = m_ClientIDToPlayerId[id];
                    T sessionPlayerData = m_ClientData[playerId];
                    sessionPlayerData.Reinitialize();
                    m_ClientData[playerId] = sessionPlayerData;
                }
            }

            foreach (var id in idsToClear)
            {
                string playerId = m_ClientIDToPlayerId[id];
                if (GetPlayerData(playerId)?.ClientID == id)
                {
                    m_ClientData.Remove(playerId);
                }

                m_ClientIDToPlayerId.Remove(id);
            }
        }
    }
}
