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
using VContainer;

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

        [Inject]
        LobbyServiceFacade m_LobbyServiceFacade;

        [Inject]
        IPublisher<ConnectionEventMessage> m_ConnectionEventPublisher;

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

                    var sessionData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(playerId);
                    if (sessionData.HasValue)
                    {
                        m_ConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.GenericDisconnect, PlayerName = sessionData.Value.PlayerName });
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
            if (m_Portal.NetManager.IsHost)
            {
                SendServerToAllClientsSetDisconnectReason(ConnectStatus.HostEndedSession);
                // Wait before shutting down to make sure clients receive that message before they are disconnected
                StartCoroutine(WaitToShutdown());
            }
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
        /// This logic plugs into the "ConnectionApprovalResponse" exposed by Netcode.NetworkManager. It is run every time a client connects to us.
        /// The complementary logic that runs when the client starts its connection can be found in ClientGameNetPortal.StartClient.
        /// </summary>
        /// <remarks>
        /// Multiple things can be done here, some asynchronously. For example, it could authenticate your user against an auth service like UGS' auth service. It can
        /// also send custom messages to connecting users before they receive their connection result (this is useful to set status messages client side
        /// when connection is refused, for example).
        /// </remarks>
        ///
        /// <param name="request"> The initial request contains, among other things, binary data passed into StartClient. In our case, this is the client's GUID,
        /// which is a unique identifier for their install of the game that persists across app restarts.
        ///  <param name="response"> Our response to the approval process. In case of connection refusal with custom return message, we delay using the Pending field.
        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData too high, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                response.Approved = false;
                return;
            }

            // Approval check happens for Host too, but obviously we want it to be approved
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, m_Portal.GetPlayerId(),
                    new SessionPlayerData(clientId, m_Portal.PlayerName, m_Portal.AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));

                response.Approved = true;
                response.CreatePlayerObject = true;
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            var gameReturnStatus = CanClientConnect(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, m_Portal.AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));
                SendServerToClientConnectResult(clientId, gameReturnStatus);

                //Populate our client scene map
                m_ClientSceneMap[clientId] = connectionPayload.clientScene;

                m_ConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.Success, PlayerName = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId)?.PlayerName });
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Position = Vector3.zero;
                response.Rotation = Quaternion.identity;
                return;
            }

            // In order for clients to not just get disconnected with no feedback, the server needs to tell the client why it disconnected it.
            // This could happen after an auth check on a service or because of gameplay reasons (server full, wrong build version, etc)
            // Since network objects haven't synced yet (still in the approval process), we need to send a custom message to clients, wait for
            // UTP to update a frame and flush that message, then give our response to NetworkManager's connection approval process, with a denied approval.
            IEnumerator WaitToDenyApproval(NetworkManager.ConnectionApprovalResponse response)
            {
                response.Pending = true; // give some time for server to send connection status message to clients
                response.Approved = false;
                SendServerToClientConnectResult(clientId, gameReturnStatus);
                SendServerToClientSetDisconnectReason(clientId, gameReturnStatus);
                yield return null; // wait a frame so UTP can flush it's messages on next update
                response.Pending = false; // connection approval process can be finished.
            }

            StartCoroutine(WaitToDenyApproval(response));
            if (m_LobbyServiceFacade.CurrentUnityLobby != null)
            {
                m_LobbyServiceFacade.RemovePlayerFromLobbyAsync(connectionPayload.playerId, m_LobbyServiceFacade.CurrentUnityLobby.Id);
            }
        }

        ConnectStatus CanClientConnect(ConnectionPayload connectionPayload)
        {
            if (m_Portal.NetManager.ConnectedClientsIds.Count >= CharSelectData.k_MaxLobbyPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.isDebug != Debug.isDebugBuild)
            {
                return ConnectStatus.IncompatibleBuildType;
            }

            return SessionManager<SessionPlayerData>.Instance.IsDuplicateConnection(connectionPayload.playerId) ? ConnectStatus.LoggedInAgain : ConnectStatus.Success;
        }

        IEnumerator WaitToShutdown()
        {
            yield return null;
            m_Portal.NetManager.Shutdown();
            SessionManager<SessionPlayerData>.Instance.OnServerEnded();
        }

        /// <summary>
        /// Sends a DisconnectReason to all connected clients. This should only be done on the server, prior to disconnecting the client.
        /// </summary>
        /// <param name="status"> The reason for the upcoming disconnect.</param>
        static void SendServerToAllClientsSetDisconnectReason(ConnectStatus status)
        {
            var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(nameof(ClientGameNetPortal.ReceiveServerToClientSetDisconnectReason_CustomMessage), writer);
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
            SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);
        }

    }
}
