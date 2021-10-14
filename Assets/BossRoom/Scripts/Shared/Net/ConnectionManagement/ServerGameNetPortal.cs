using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom.Client;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Server
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
        [SerializeField]
        NetworkObject m_GameState;

        private GameNetPortal m_Portal;

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

        /// <summary>
        /// Keeps a list of what clients are in what scenes.
        /// </summary>
        private Dictionary<ulong, int> m_ClientSceneMap = new Dictionary<ulong, int>();

        /// <summary>
        /// The active server scene index.
        /// </summary>
        public int ServerScene { get { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex; } }

        void Start()
        {
            m_Portal = GetComponent<GameNetPortal>();

            // we add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious Netcode for GameObjects (Netcode)
            // warning: "No ConnectionApproval callback defined. Connection approval will timeout"
            m_Portal.NetManager.ConnectionApprovalCallback += ApprovalCheck;
            m_Portal.NetManager.OnServerStarted += ServerStartedHandler;
            m_ClientData = new Dictionary<string, PlayerData>();
            m_ClientIDToGuid = new Dictionary<ulong, string>();
        }

        void OnDestroy()
        {
            if( m_Portal != null )
            {
                if( m_Portal.NetManager != null)
                {
                    m_Portal.NetManager.ConnectionApprovalCallback -= ApprovalCheck;
                    m_Portal.NetManager.OnServerStarted -= ServerStartedHandler;
                }
            }
        }

        public void OnNetworkReady()
        {
            if (!m_Portal.NetManager.IsServer)
            {
                enabled = false;
            }
            else
            {
                //O__O if adding any event registrations here, please add an unregistration in OnClientDisconnect.
                m_Portal.NetManager.OnClientDisconnectCallback += OnClientDisconnect;

                //The "BossRoom" server always advances to CharSelect immediately on start. Different games
                //may do this differently.
                NetworkManager.Singleton.SceneManager.LoadScene("CharSelect", LoadSceneMode.Single);

                if( m_Portal.NetManager.IsHost)
                {
                    m_ClientSceneMap[m_Portal.NetManager.LocalClientId] = ServerScene;
                }
            }
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            m_ClientSceneMap.Remove(clientId);
            if( m_ClientIDToGuid.TryGetValue(clientId, out var guid ) )
            {
                m_ClientIDToGuid.Remove(clientId);

                if( m_ClientData[guid].m_ClientID == clientId )
                {
                    //be careful to only remove the ClientData if it is associated with THIS clientId; in a case where a new connection
                    //for the same GUID kicks the old connection, this could get complicated. In a game that fully supported the reconnect flow,
                    //we would NOT remove ClientData here, but instead time it out after a certain period, since the whole point of it is
                    //to remember client information on a per-guid basis after the connection has been lost.
                    m_ClientData.Remove(guid);
                }
            }

            if( clientId == m_Portal.NetManager.LocalClientId )
            {
                //the ServerGameNetPortal may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                m_Portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        public void OnClientSceneChanged(ulong clientId, int sceneIndex)
        {
            m_ClientSceneMap[clientId] = sceneIndex;
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
            m_ClientSceneMap.Clear();
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
            return (playerData != null) ? playerData.Value.m_PlayerName : ("Player" + playerNum);
        }


        /// <summary>
        /// This logic plugs into the "ConnectionApprovalCallback" exposed by Netcode.NetworkManager, and is run every time a client connects to us.
        /// See ClientGameNetPortal.StartClient for the complementary logic that runs when the client starts its connection.
        /// </summary>
        /// <remarks>
        /// Since our game doesn't have to interact with some third party authentication service to validate the identity of the new connection, our ApprovalCheck
        /// method is simple, and runs synchronously, invoking "callback" to signal approval at the end of the method. Netcode currently doesn't support the ability
        /// to send back more than a "true/false", which means we have to work a little harder to provide a useful error return to the client. To do that, we invoke a
        /// custom message in the same channel that Netcode uses for its connection callback. Since the delivery is NetworkDelivery.ReliableSequenced, we can be
        /// confident that our login result message will execute before any disconnect message.
        /// </remarks>
        /// <param name="connectionData">binary data passed into StartClient. In our case this is the client's GUID, which is a unique identifier for their install of the game that persists across app restarts. </param>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="connectionApprovedCallback">The delegate we must invoke to signal that the connection was approved or not. </param>
        private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            if (connectionData.Length > k_MaxConnectPayload)
            {
                connectionApprovedCallback(false, 0, false, null, null);
                return;
            }

            // Approval check happens for Host too, but obviously we want it to be approved
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                connectionApprovedCallback(true, null, true, null, null);
                return;
            }

            ConnectStatus gameReturnStatus = ConnectStatus.Success;

            // Test for over-capacity connection. This needs to be done asap, to make sure we refuse connections asap and don't spend useless time server side
            // on invalid users trying to connect
            // todo this is currently still spending too much time server side.
            if (m_ClientData.Count >= CharSelectData.k_MaxLobbyPlayers)
            {
                gameReturnStatus = ConnectStatus.ServerFull;
                //TODO-FIXME:Netcode Issue #796. We should be able to send a reason and disconnect without a coroutine delay.
                //TODO:Netcode: In the future we expect Netcode to allow us to return more information as part of
                //the approval callback, so that we can provide more context on a reject. In the meantime we must provide the extra information ourselves,
                //and then manually close down the connection.
                SendServerToClientConnectResult(clientId, gameReturnStatus);
                SendServerToClientSetDisconnectReason(clientId, gameReturnStatus);
                StartCoroutine(WaitToDisconnect(clientId));
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html

            int clientScene = connectionPayload.clientScene;

            Debug.Log("Host ApprovalCheck: connecting client GUID: " + connectionPayload.clientGUID);

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
                    ulong oldClientId = m_ClientData[connectionPayload.clientGUID].m_ClientID;
                    // kicking old client to leave only current
                    SendServerToClientSetDisconnectReason(oldClientId, ConnectStatus.LoggedInAgain);
                    StartCoroutine(WaitToDisconnect(clientId));
                    return;
                }
            }

            SendServerToClientConnectResult(clientId, gameReturnStatus);

            //Populate our dictionaries with the playerData
            m_ClientSceneMap[clientId] = clientScene;
            m_ClientIDToGuid[clientId] = connectionPayload.clientGUID;
            m_ClientData[connectionPayload.clientGUID] = new PlayerData(connectionPayload.playerName, clientId);

            connectionApprovedCallback(true, null, true, Vector3.zero, Quaternion.identity);

            // connection approval will create a player object for you
            AssignPlayerName(clientId, connectionPayload.playerName);
        }

        private IEnumerator WaitToDisconnect(ulong clientId)
        {
            yield return new WaitForSeconds(0.5f);
            m_Portal.NetManager.DisconnectClient(clientId);
        }

        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        public void SendServerToClientSetDisconnectReason(ulong clientID, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientSetDisconnectReason_CustomMessage), clientID, writer);
        }

        /// <summary>
        /// Responsible for the Server->Client custom message of the connection result.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> the status to pass to the client</param>
        public void SendServerToClientConnectResult(ulong clientID, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientConnectResult_CustomMessage), clientID, writer);
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        private void ServerStartedHandler()
        {
            m_ClientData.Add("host_guid", new PlayerData(m_Portal.PlayerName, NetworkManager.Singleton.LocalClientId));
            m_ClientIDToGuid.Add(NetworkManager.Singleton.LocalClientId, "host_guid");

            AssignPlayerName(NetworkManager.Singleton.LocalClientId, m_Portal.PlayerName);

            // server spawns game state
            var gameState = Instantiate(m_GameState);

            gameState.Spawn();
        }

        static void AssignPlayerName(ulong clientId, string playerName)
        {
            // get this client's player NetworkObject
            var networkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            // update client's name
            if (networkObject.TryGetComponent(out PersistentPlayer persistentPlayer))
            {
                persistentPlayer.NetworkNameState.Name.Value = playerName;
            }
        }
    }
}
