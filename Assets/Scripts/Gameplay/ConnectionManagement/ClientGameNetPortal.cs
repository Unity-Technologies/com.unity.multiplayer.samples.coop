using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Client side logic for a GameNetPortal. Contains implementations for all of GameNetPortal's S2C RPCs.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour
    {
        public static ClientGameNetPortal Instance;
        private GameNetPortal m_Portal;

        /// <summary>
        /// If a disconnect occurred this will be populated with any contextual information that was available to explain why.
        /// </summary>
        public DisconnectReason DisconnectReason { get; } = new DisconnectReason();

        const int k_NbReconnectAttempts = 2;

        Coroutine m_TryToReconnectCoroutine;

        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPub;
        IPublisher<ConnectStatus> m_ConnectStatusPub;
        IPublisher<ReconnectMessage> m_ReconnectMessagePub;

        [Inject]
        private void InjectDependencies(LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby, IPublisher<QuitGameSessionMessage> quitGameSessionPub, IPublisher<ConnectStatus> connectStatusPub, IPublisher<ReconnectMessage> reconnectMessagePub)
        {
            m_QuitGameSessionPub = quitGameSessionPub;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_ConnectStatusPub = connectStatusPub;

            m_ReconnectMessagePub = reconnectMessagePub;
        }

        private void Awake()
        {
            if (Instance != null) throw new Exception("Invalid state, instance is not null");

            Instance = this;
        }

        void Start()
        {
            m_Portal = GetComponent<GameNetPortal>();

            m_Portal.NetManager.OnClientDisconnectCallback += OnDisconnectOrTimeout;
        }

        void OnDestroy()
        {
            if (m_Portal != null)
            {
                if (m_Portal.NetManager != null)
                {
                    m_Portal.NetManager.OnClientDisconnectCallback -= OnDisconnectOrTimeout;
                }

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
                {
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage));
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage));
                }
            }

            Instance = null;
        }

        public void OnNetworkReady()
        {
            if (!m_Portal.NetManager.IsClient)
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Invoked when the user has requested a disconnect via the UI, e.g. when hitting "Return to Main Menu" in the post-game scene.
        /// </summary>
        public void OnUserDisconnectRequest()
        {
            if (m_Portal.NetManager.IsClient)
            {
                DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);

                if (m_TryToReconnectCoroutine != null)
                {
                    StopCoroutine(m_TryToReconnectCoroutine);
                    m_TryToReconnectCoroutine = null;
                    m_ReconnectMessagePub.Publish(new ReconnectMessage(k_NbReconnectAttempts, k_NbReconnectAttempts));
                }
                // If we are the server, shutdown will be handled by ServerGameNetPortal
                if (!m_Portal.NetManager.IsServer)
                {
                    m_Portal.NetManager.Shutdown();
                }
            }
        }

        public void OnConnectFinished(ConnectStatus status)
        {
            //on success, there is nothing to do (the Netcode for GameObjects (Netcode) scene management system will take us to the next scene).
            //on failure, we must raise an event so that the UI layer can display something.
            Debug.Log("RecvConnectFinished Got status: " + status);

            if (status != ConnectStatus.Success)
            {
                //this indicates a game level failure, rather than a network failure. See note in ServerGameNetPortal.
                DisconnectReason.SetDisconnectReason(status);
            }
            else
            {
                if (m_TryToReconnectCoroutine != null)
                {
                    StopCoroutine(m_TryToReconnectCoroutine);
                    m_TryToReconnectCoroutine = null;
                    m_ReconnectMessagePub.Publish(new ReconnectMessage(k_NbReconnectAttempts, k_NbReconnectAttempts));
                }
                m_ConnectStatusPub.Publish(status);
                if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                {
                    m_LobbyServiceFacade.BeginTracking();
                }
            }
        }

        private void OnDisconnectReasonReceived(ConnectStatus status)
        {
            DisconnectReason.SetDisconnectReason(status);
        }

        private void OnDisconnectOrTimeout(ulong clientID)
        {
            // This is also called on the Host when a different client disconnects. To make sure we only handle our own disconnection, verify that we are either
            // not a host (in which case we know this is about us) or that the clientID is the same as ours if we are the host.
            if (!NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsHost && NetworkManager.Singleton.LocalClientId == clientID)
            {
                //On a client disconnect we want to take them back to the main menu.
                //We have to check here in SceneManager if our active scene is the main menu, as if it is, it means we timed out rather than a raw disconnect;
                switch (DisconnectReason.Reason)
                {
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.HostEndedSession:
                    case ConnectStatus.ServerFull:
                    case ConnectStatus.IncompatibleBuildType:

                        m_QuitGameSessionPub.Publish(new QuitGameSessionMessage() { UserRequested = false }); // go through the normal leave flow
                        break;
                    case ConnectStatus.LoggedInAgain:
                        if (m_TryToReconnectCoroutine == null)
                        {
                            m_QuitGameSessionPub.Publish(new QuitGameSessionMessage() { UserRequested = false }); // if not trying to reconnect, go through the normal leave flow
                        }
                        break;
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.Undefined:
                        DisconnectReason.SetDisconnectReason(ConnectStatus.Reconnecting);
                        var lobbyCode = "";
                        if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                        {
                            lobbyCode = m_LobbyServiceFacade.CurrentUnityLobby.LobbyCode;
                        }
                        // try reconnecting
                        m_TryToReconnectCoroutine ??= StartCoroutine(TryToReconnect(lobbyCode));
                        break;
                    default:
                        throw new NotImplementedException(DisconnectReason.Reason.ToString());
                }

                m_ConnectStatusPub.Publish(DisconnectReason.Reason);
                DisconnectReason.Clear();
            }
        }

        private IEnumerator TryToReconnect(string lobbyCode)
        {
            Debug.Log("Lost connection to host, trying to reconnect...");
            int nbTries = 0;
            while (nbTries < k_NbReconnectAttempts)
            {
                NetworkManager.Singleton.Shutdown();

                yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress); // wait until NetworkManager completes shutting down
                Debug.Log($"Reconnecting attempt {nbTries + 1}/{k_NbReconnectAttempts}...");
                m_ReconnectMessagePub.Publish(new ReconnectMessage(nbTries, k_NbReconnectAttempts));
                if (!string.IsNullOrEmpty(lobbyCode))
                {
                    var leavingLobby = m_LobbyServiceFacade.EndTracking();
                    yield return new WaitUntil(() => leavingLobby.IsCompleted);
                    var joiningLobby = m_LobbyServiceFacade.TryJoinLobbyAsync("", lobbyCode);
                    yield return new WaitUntil(() => joiningLobby.IsCompleted);
                    if (joiningLobby.Result.Success)
                    {
                        m_LobbyServiceFacade.SetRemoteLobby(joiningLobby.Result.Lobby);
                        var joiningRelay = StartClientUnityRelayModeAsync(null);
                        yield return new WaitUntil(() => joiningRelay.IsCompleted);
                    }
                    else
                    {
                        Debug.Log("Failed joining lobby.");
                    }
                }
                else
                {
                    ConnectClient();
                }
                yield return new WaitForSeconds(1.1f * NetworkManager.Singleton.NetworkConfig.ClientConnectionBufferTimeout + ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).DisconnectTimeoutMS / 1000.0f); // wait a bit longer than the timeout duration to make sure we have enough time to stop this coroutine if successful
                nbTries++;
            }

            // If the coroutine has not been stopped before this, it means we failed to connect during all attempts
            Debug.Log("All tries failed, returning to main menu");
            m_QuitGameSessionPub.Publish(new QuitGameSessionMessage() { UserRequested = false });
            if (!DisconnectReason.HasTransitionReason)
            {
                DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);
            }
            m_TryToReconnectCoroutine = null;
            m_ReconnectMessagePub.Publish(new ReconnectMessage(k_NbReconnectAttempts, k_NbReconnectAttempts));
            m_ConnectStatusPub.Publish(DisconnectReason.Reason);
        }

        /// <summary>
        /// Wraps the invocation of NetworkManager.StartClient, including our GUID as the payload.
        /// </summary>
        /// <remarks>
        /// This method must be static because, when it is invoked, the client still doesn't know it's a client yet, and in particular, GameNetPortal hasn't
        /// yet initialized its client and server GameNetPortal objects yet (which it does in OnNetworkSpawn, based on the role that the current player is performing).
        /// </remarks>
        /// <param name="ipaddress">the IP address of the host to connect to. (currently IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public void StartClient(string ipaddress, int port)
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);

            ConnectClient();
        }

        public async Task StartClientUnityRelayModeAsync(Action<string> onFailure)
        {
            Debug.Log($"Setting Unity Relay client with join code {m_LocalLobby.RelayJoinCode}");

            try
            {
                var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) =
                    await UnityRelayUtilities.JoinRelayServerFromJoinCode(m_LocalLobby.RelayJoinCode);

                await m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(allocationIdBytes.ToString(), m_LocalLobby.RelayJoinCode);
                var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                utp.SetClientRelayData(ipv4Address, port, allocationIdBytes, key, connectionData, hostConnectionData, isSecure: true);
            }
            catch (Exception e)
            {
                onFailure?.Invoke(e.Message);
                return;//not re-throwing, but still not allowing to connect
            }

            ConnectClient();
        }

        void ConnectClient()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = m_Portal.GetPlayerId(),
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = m_Portal.PlayerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_Portal.NetManager.NetworkConfig.ConnectionData = payloadBytes;

            //and...we're off! Netcode will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by getting an ReceiveServerToClientSetDisconnectReason_CustomMessage callback for ourselves and get a message telling us the reason
            //  If the socket connection succeeds, we'll get our ReceiveServerToClientConnectResult_CustomMessage invoked. This is where game-layer failures will be reported.
            m_Portal.NetManager.StartClient();
            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();

            // should only do this once StartClient has been called (start client will initialize CustomMessagingManager
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage), ReceiveServerToClientConnectResult_CustomMessage);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), ReceiveServerToClientSetDisconnectReason_CustomMessage);
        }

        public static void ReceiveServerToClientConnectResult_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);
            Instance.OnConnectFinished(status);
        }

        public static void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);
            Instance.OnDisconnectReasonReceived(status);
        }
    }
}
