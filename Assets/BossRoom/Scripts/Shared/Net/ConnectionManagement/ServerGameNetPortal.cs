using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server logic plugin for the GameNetHub. Contains implementations for all GameNetHub's C2S RPCs.
    /// </summary>
    public class ServerGameNetPortal : MonoBehaviour
    {
        [SerializeField]
        NetworkObject m_GameState;

        GameNetPortal m_Portal;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        const int k_MaxConnectPayload = 1024;

        /// <summary>
        /// Keeps a list of what clients are in what scenes.
        /// </summary>
        Dictionary<ulong, int> m_ClientSceneMap = new Dictionary<ulong, int>();

        /// <summary>
        /// The active server scene index.
        /// </summary>
        static int ServerScene => SceneManager.GetActiveScene().buildIndex;

        LobbyServiceFacade m_LobbyServiceFacade;

        [Inject]
        void InjectDependencies(LobbyServiceFacade lobbyServiceFacade)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
        }

        void Start()
        {
            m_Portal = GetComponent<GameNetPortal>();

            // we add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious Netcode for GameObjects (Netcode)
            // warning: "No ConnectionApproval callback defined. Connection approval will timeout"
            m_Portal.NetManager.ConnectionApprovalCallback += ApprovalCheck;
            m_Portal.NetManager.OnServerStarted += ServerStartedHandler;
        }

        void OnDestroy()
        {
            if (m_Portal != null)
            {
                if (m_Portal.NetManager != null)
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

                if (m_Portal.NetManager.IsHost)
                {
                    m_ClientSceneMap[m_Portal.NetManager.LocalClientId] = ServerScene;
                }
            }
        }

        /// <summary>
        /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
        /// and the server is stopped."
        /// </summary>
        void OnClientDisconnect(ulong clientId)
        {
            m_ClientSceneMap.Remove(clientId);

            if (clientId == m_Portal.NetManager.LocalClientId)
            {
                //the ServerGameNetPortal may be initialized again, which will cause its OnNetworkSpawn to be called again.
                //Consequently we need to unregister anything we registered, when the NetworkManager is shutting down.
                m_Portal.NetManager.OnClientDisconnectCallback -= OnClientDisconnect;
                if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                {
                    m_LobbyServiceFacade.DeleteLobbyAsync(m_LobbyServiceFacade.CurrentUnityLobby.Id);
                }
                SessionManager<SessionPlayerData>.Instance.OnServerEnded();
            }
            else
            {
                var playerId = SessionManager<SessionPlayerData>.Instance.GetPlayerId(clientId);
                if (playerId != null)
                {
                    if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                    {
                        m_LobbyServiceFacade.RemovePlayerFromLobbyAsync(playerId, m_LobbyServiceFacade.CurrentUnityLobby.Id);
                    }
                    SessionManager<SessionPlayerData>.Instance.DisconnectClient(clientId);
                }
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

        void Clear()
        {
            //resets all our runtime state.
            m_ClientSceneMap.Clear();
        }

        public bool AreAllClientsInServerScene()
        {
            foreach (var kvp in m_ClientSceneMap)
            {
                if (kvp.Value != ServerScene) { return false; }
            }

            return true;
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
        void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData too high, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                connectionApprovedCallback(false, 0, false, null, null);
                return;
            }

            // Approval check happens for Host too, but obviously we want it to be approved
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, m_Portal.GetPlayerId(),
                    new SessionPlayerData(clientId, m_Portal.PlayerName, m_Portal.AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));

                connectionApprovedCallback(true, null, true, null, null);
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            var gameReturnStatus = GetConnectStatus(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, m_Portal.AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));
                SendServerToClientConnectResult(clientId, gameReturnStatus);

                //Populate our client scene map
                m_ClientSceneMap[clientId] = connectionPayload.clientScene;

                connectionApprovedCallback(true, null, true, Vector3.zero, Quaternion.identity);
                // connection approval will create a player object for you
            }
            else
            {
                //TODO-FIXME:Netcode Issue #796. We should be able to send a reason and disconnect without a coroutine delay.
                //TODO:Netcode: In the future we expect Netcode to allow us to return more information as part of the
                //approval callback, so that we can provide more context on a reject. In the meantime we must provide
                //the extra information ourselves, and then wait a short time before manually close down the connection.
                SendServerToClientConnectResult(clientId, gameReturnStatus);
                SendServerToClientSetDisconnectReason(clientId, gameReturnStatus);
                StartCoroutine(WaitToDenyApproval(connectionApprovedCallback));
                if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                {
                    m_LobbyServiceFacade.RemovePlayerFromLobbyAsync(connectionPayload.playerId, m_LobbyServiceFacade.CurrentUnityLobby.Id);
                }
            }
        }

        ConnectStatus GetConnectStatus(ConnectionPayload connectionPayload)
        {
            if (m_Portal.NetManager.ConnectedClientsIds.Count >= CharSelectData.k_MaxLobbyPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.isDebug != Debug.isDebugBuild)
            {
                return ConnectStatus.IncompatibleBuildType;
            }

            return SessionManager<SessionPlayerData>.Instance.IsDuplicateConnection(connectionPayload.playerId) ?
                ConnectStatus.LoggedInAgain : ConnectStatus.Success;
        }

        static IEnumerator WaitToDenyApproval(NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            yield return new WaitForSeconds(0.5f);
            connectionApprovedCallback(false, 0, false, null, null);
        }

        /// <summary>
        /// Sends a DisconnectReason to the indicated client. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="clientID"> id of the client to send to </param>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        static void SendServerToClientSetDisconnectReason(ulong clientID, ConnectStatus status)
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
        static void SendServerToClientConnectResult(ulong clientID, ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ClientGameNetPortal.ReceiveServerToClientConnectResult_CustomMessage), clientID, writer);
        }

        /// <summary>
        /// Called after the server is created-  This is primarily meant for the host server to clean up or handle/set state as its starting up
        /// </summary>
        void ServerStartedHandler()
        {
            // server spawns game state
            var gameState = Instantiate(m_GameState);

            gameState.Spawn();

            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();

            //The "BossRoom" server always advances to CharSelect immediately on start. Different games
            //may do this differently.
            SceneLoaderWrapper.Instance.LoadScene("CharSelect");
        }

    }
}
