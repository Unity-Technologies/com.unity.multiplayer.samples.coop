using System;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI.Transports.LiteNetLib;
using MLAPI.Transports.PhotonRealtime;
using Photon.Realtime;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Client side logic for a GameNetPortal. Contains implementations for all of GameNetPortal's S2C RPCs.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour
    {
        private GameNetPortal m_Portal;

        /// <summary>
        /// If a disconnect occurred this will be populated with any contextual information that was available to explain why.
        /// </summary>
        public DisconnectReason DisconnectReason { get; private set; } = new DisconnectReason();

        /// <summary>
        /// Time in seconds before the client considers a lack of server response a timeout
        /// </summary>
        private const int k_TimeoutDuration = 10;

        public event Action<ConnectStatus> ConnectFinished;

        /// <summary>
        /// This event fires when the client sent out a request to start the client, but failed to hear back after an allotted amount of
        /// time from the host.
        /// </summary>
        public event Action NetworkTimedOut;

        void Start()
        {
            m_Portal = GetComponent<GameNetPortal>();

            m_Portal.NetworkReadied += OnNetworkReady;
            m_Portal.ConnectFinished += OnConnectFinished;
            m_Portal.DisconnectReasonReceived += OnDisconnectReasonReceived;
            m_Portal.NetManager.OnClientDisconnectCallback += OnDisconnectOrTimeout;
        }

        void OnDestroy()
        {
            if( m_Portal != null )
            {
                m_Portal.NetworkReadied -= OnNetworkReady;
                m_Portal.ConnectFinished -= OnConnectFinished;
                m_Portal.DisconnectReasonReceived -= OnDisconnectReasonReceived;

                if( m_Portal.NetManager != null )
                {
                    m_Portal.NetManager.OnClientDisconnectCallback -= OnDisconnectOrTimeout;
                }
            }
        }

        private void OnNetworkReady()
        {
            if (!m_Portal.NetManager.IsClient)
            {
                enabled = false;
            }
            else
            {
                // O__O if adding any event registrations in this block, please add unregistrations in the OnClientDisconnect method.
                if(!m_Portal.NetManager.IsHost )
                {
                    //only do this if a pure client, so as not to overlap with host behavior in ServerGameNetPortal.
                    m_Portal.UserDisconnectRequested += OnUserDisconnectRequest;
                }
            }
        }

        /// <summary>
        /// Invoked when the user has requested a disconnect via the UI, e.g. when hitting "Return to Main Menu" in the post-game scene.
        /// </summary>
        private void OnUserDisconnectRequest()
        {
            if( m_Portal.NetManager.IsClient )
            {
                DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);
                m_Portal.NetManager.Shutdown();
            }
        }

        private void OnConnectFinished(ConnectStatus status)
        {
            //on success, there is nothing to do (the Netcode for GameObjects (Netcode) scene management system will take us to the next scene).
            //on failure, we must raise an event so that the UI layer can display something.
            Debug.Log("RecvConnectFinished Got status: " + status);

            if( status != ConnectStatus.Success )
            {
                //this indicates a game level failure, rather than a network failure. See note in ServerGameNetPortal.
                DisconnectReason.SetDisconnectReason(status);
            }

            ConnectFinished?.Invoke(status);
        }

        private void OnDisconnectReasonReceived(ConnectStatus status)
        {
            DisconnectReason.SetDisconnectReason(status);
        }

        private void OnDisconnectOrTimeout(ulong clientID)
        {
            // we could also check whether the disconnect was us or the host, but the "interesting" question is whether
            //following the disconnect, we're no longer a Connected Client, so we just explicitly check that scenario.
            if ( !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost )
            {
                m_Portal.UserDisconnectRequested -= OnUserDisconnectRequest;

                //On a client disconnect we want to take them back to the main menu.
                //We have to check here in SceneManager if our active scene is the main menu, as if it is, it means we timed out rather than a raw disconnect;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                {
                    // we're not at the main menu, so we obviously had a connection before... thus, we aren't in a timeout scenario.
                    // Just shut down networking and switch back to main menu.
                    NetworkManager.Singleton.Shutdown();
                    if( !DisconnectReason.HasTransitionReason )
                    {
                        //disconnect that happened for some other reason than user UI interaction--should display a message.
                        DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);
                    }
                    SceneManager.LoadScene("MainMenu");
                }
                else
                {
                    NetworkTimedOut?.Invoke();
                }
            }
        }

        /// <summary>
        /// Wraps the invocation of NetworkManager.StartClient, including our GUID as the payload.
        /// </summary>
        /// <remarks>
        /// This method must be static because, when it is invoked, the client still doesn't know it's a client yet, and in particular, GameNetPortal hasn't
        /// yet initialized its client and server GNP-Logic objects yet (which it does in OnNetworkSpawn, based on the role that the current player is performing).
        /// </remarks>
        /// <param name="portal"> </param>
        /// <param name="ipaddress">the IP address of the host to connect to. (currently IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public static void StartClient(GameNetPortal portal, string ipaddress, int port)
        {
            var chosenTransport = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().IpHostTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            switch (chosenTransport)
            {
                case LiteNetLibTransport liteNetLibTransport:
                    liteNetLibTransport.Address = ipaddress;
                    liteNetLibTransport.Port = (ushort)port;
                    break;
                case UNetTransport unetTransport:
                    unetTransport.ConnectAddress = ipaddress;
                    unetTransport.ConnectPort = port;
                    break;
                case UnityTransport unityTransport:
                    // TODO: once this is exposed in the adapter we will be able to change it
                    // UnityTransport.Address = ipaddress;
                    // UnityTransport.Port = (ushort)port;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(chosenTransport));
            }

            ConnectClient(portal);
        }

        /// <summary>
        /// Wraps the invocation of NetworkingManager.StartClient, including our GUID as the payload.
        /// </summary>
        /// <remarks>
        /// Relay version of <see cref="StartClient"/>, see start client remarks for more information.
        /// </remarks>
        /// <param name="portal"> </param>
        /// <param name="roomKey">The room name of the host to connect to.</param>
        public static bool StartClientRelayMode(GameNetPortal portal, string roomKey, out string failMessage)
        {
            var splits = roomKey.Split('_');

            if (splits.Length != 2)
            {
                failMessage = "Malformatted Room Key!";
                return false;
            }

            var region = splits[0];
            var roomName = splits[1];

            var chosenTransport  = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().RelayTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            switch (chosenTransport)
            {
                case PhotonRealtimeTransport photonRealtimeTransport:
                    photonRealtimeTransport.RoomName = roomName;
                    PhotonAppSettings.Instance.AppSettings.FixedRegion = region;
                    break;
                default:
                    throw new Exception($"unhandled relay transport {chosenTransport.GetType()}");
            }

            ConnectClient(portal);

            failMessage = String.Empty;
            return true;
        }

        public static async void StartClientUnityRelayModeAsync(GameNetPortal portal, string joinCode)
        {

            var chosenTransport  = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().UnityRelayTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            switch (chosenTransport)
            {
                case UnityTransport utp:
                    Debug.Log($"Setting Unity Relay client with join code {joinCode}");
                    try
                    {
                        await UnityServices.InitializeAsync();
                        Debug.Log(AuthenticationService.Instance);

                        if (!AuthenticationService.Instance.IsSignedIn)
                        {
                            await AuthenticationService.Instance.SignInAnonymouslyAsync();
                            var playerId = AuthenticationService.Instance.PlayerId;
                            Debug.Log(playerId);
                        }

                        var clientRelayUtilityTask =  RelayUtility.JoinRelayServerFromJoinCode(joinCode);
                        await clientRelayUtilityTask;
                        var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) = clientRelayUtilityTask.Result;
                        utp.SetRelayServerData(ipv4Address, port, allocationIdBytes, key, connectionData, hostConnectionData);
                    }
                    catch (Exception e)
                    {
                        var menuUI = MainMenuUI.Instance;
                        if (menuUI)
                        {
                            menuUI.PushConnectionResponsePopup("Unity Relay: Join Failed", $"{e.Message}", true, true);
                        }
                        throw;
                    }

                    break;
                default:
                    throw new Exception($"unhandled relay transport {chosenTransport.GetType()}");
            }

            ConnectClient(portal);
        }

        private static void ConnectClient(GameNetPortal portal)
        {
            var clientGuid = ClientPrefs.GetGuid();
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                clientGUID = clientGuid,
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = portal.PlayerName
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            portal.NetManager.NetworkConfig.ConnectionData = payloadBytes;
            portal.NetManager.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

            //and...we're off! Netcode will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by getting an OnClientDisconnect callback for ourselves (TODO-FIXME:Netcode GOMPS-79, provide feedback for different transport failures).
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported.
            portal.NetManager.StartClient();
        }
    }
}
