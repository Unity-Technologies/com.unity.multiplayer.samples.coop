using System;
using System.Collections;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;

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
        Coroutine m_TryToReconnectCoroutine;
        string m_JoinCode;
        OnlineMode m_OnlineMode;

        /// <summary>
        /// If a disconnect occurred this will be populated with any contextual information that was available to explain why.
        /// </summary>
        public DisconnectReason DisconnectReason { get; } = new DisconnectReason();

        /// <summary>
        /// Time in seconds before the client considers a lack of server response a timeout
        /// </summary>
        private const int k_TimeoutDuration = 10;

        /// <summary>
        /// This event fires when the client sent out a request to start the client, but failed to hear back after an allotted amount of
        /// time from the host.
        /// </summary>
        public event Action NetworkTimedOut;

        private LobbyServiceFacade m_LobbyServiceFacade;
        IPublisher<ConnectStatus> m_ConnectStatusPub;

        [Inject]
        private void InjectDependencies(LobbyServiceFacade lobbyServiceFacade, IPublisher<ConnectStatus> connectStatusPub)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_ConnectStatusPub = connectStatusPub;
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
                }
                m_ConnectStatusPub.Publish(status);
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
                if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    if (DisconnectReason.Reason == ConnectStatus.UserRequestedDisconnect || NetworkManager.Singleton.IsHost)
                    {
                        // simply shut down and go back to main menu
                        NetworkManager.Singleton.Shutdown();
                        SceneManager.LoadScene("MainMenu");
                    }
                    else
                    {
                        DisconnectReason.SetDisconnectReason(ConnectStatus.Reconnecting);
                        // load new scene to workaround MTT-2684
                        SceneManager.LoadSceneAsync("Loading");
                        // try reconnecting
                        m_TryToReconnectCoroutine ??= StartCoroutine(TryToReconnect());
                    }
                }
                else if (DisconnectReason.Reason == ConnectStatus.GenericDisconnect || DisconnectReason.Reason == ConnectStatus.Undefined)
                {
                    // only call this if generic disconnect. Else if there's a reason, there's already code handling that popup
                    NetworkTimedOut?.Invoke();
                }

                m_ConnectStatusPub.Publish(DisconnectReason.Reason);
                DisconnectReason.Clear();
            }
        }

        private IEnumerator TryToReconnect()
        {
            Debug.Log("Lost connection to host, trying to reconnect...");
            int nbTries = 0;
            while (nbTries < 3)
            {
                NetworkManager.Singleton.Shutdown();
                yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress); // wait until NetworkManager completes shutting down
                Debug.Log($"Reconnecting attempt {nbTries + 1}/3...");
                ConnectClient(null);
                yield return new WaitForSeconds(1.1f * k_TimeoutDuration); // wait a bit longer than the timeout duration to make sure we have enough time to stop this coroutine if successful
                nbTries++;
            }

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("All tries failed, returning to main menu");
                SceneManager.LoadScene("MainMenu");
                if (!DisconnectReason.HasTransitionReason)
                {
                    DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);
                }

            }
            NetworkManager.Singleton.Shutdown();
            m_TryToReconnectCoroutine = null;
            m_ConnectStatusPub.Publish(DisconnectReason.Reason);
        }

        /// <summary>
        /// Wraps the invocation of NetworkManager.StartClient, including our GUID as the payload.
        /// </summary>
        /// <remarks>
        /// This method must be static because, when it is invoked, the client still doesn't know it's a client yet, and in particular, GameNetPortal hasn't
        /// yet initialized its client and server GNP-Logic objects yet (which it does in OnNetworkSpawn, based on the role that the current player is performing).
        /// </remarks>
        /// <param name="ipaddress">the IP address of the host to connect to. (currently IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public void StartClient(string ipaddress, int port)
        {
            m_OnlineMode = OnlineMode.IpHost;
            var chosenTransport = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().IpHostTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            switch (chosenTransport)
            {
                case UNetTransport unetTransport:
                    unetTransport.ConnectAddress = ipaddress;
                    unetTransport.ConnectPort = port;
                    break;
                case UnityTransport unityTransport:
                    // TODO: once this is exposed in the adapter we will be able to change it
                    unityTransport.SetConnectionData(ipaddress, (ushort)port);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(chosenTransport));
            }

            ConnectClient(null);
        }

        public void StartClientUnityRelayModeAsync(string joinCode, Action<string> onFailure)
        {
            m_OnlineMode = OnlineMode.UnityRelay;
            m_JoinCode = joinCode;
            var utp = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().UnityRelayTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = utp;

            Debug.Log($"Setting Unity Relay client with join code {joinCode}");

            ConnectClient(onFailure);
        }

        async void ConnectClient(Action<string> onFailure)
        {
            if (m_OnlineMode == OnlineMode.UnityRelay)
            {
                try
                {
                    var clientRelayUtilityTask =  UnityRelayUtilities.JoinRelayServerFromJoinCode(m_JoinCode);
                    await clientRelayUtilityTask;
                    var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) = clientRelayUtilityTask.Result;

                    m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(allocationIdBytes.ToString(), m_JoinCode, null, null);
                    var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                    utp.SetRelayServerData(ipv4Address, port, allocationIdBytes, key, connectionData, hostConnectionData);
                }
                catch (Exception e)
                {
                    onFailure?.Invoke(e.Message);
                    return;//not re-throwing, but still not allowing to connect
                }
            }

            var clientGuid = ClientPrefs.GetGuid();
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                clientGUID = clientGuid,
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = m_Portal.PlayerName
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_Portal.NetManager.NetworkConfig.ConnectionData = payloadBytes;
            m_Portal.NetManager.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

            //and...we're off! Netcode will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by getting an OnClientDisconnect callback for ourselves and get a message telling us the reason
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported.
            m_Portal.NetManager.StartClient();

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
