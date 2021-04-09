using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Transports;
using MLAPI.Transports.LiteNetLib;
using MLAPI.Transports.PhotonRealtime;
using MLAPI.Transports.UNET;
using Photon.Realtime;

namespace BossRoom.Client
{
    /// <summary>
    /// Client side logic for a GameNetPortal. Contains implementations for all of GameNetPortal's S2C RPCs.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour
    {
        private GameNetPortal m_Portal;

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
            m_Portal.NetworkStarted += NetworkStart;
            m_Portal.ConnectFinished += OnConnectFinished;
            m_Portal.NetManager.OnClientDisconnectCallback += OnDisconnectOrTimeout;
        }

        private void NetworkStart()
        {
            if (!m_Portal.NetManager.IsClient)
            {
                enabled = false;
            }
            else
            {
                SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) =>
                {
                    m_Portal.C2SSceneChanged(SceneManager.GetActiveScene().buildIndex);
                };
            }
        }

        private void OnConnectFinished(ConnectStatus status)
        {
            //on success, there is nothing to do (the MLAPI scene management system will take us to the next scene).
            //on failure, we must raise an event so that the UI layer can display something.
            Debug.Log("RecvConnectFinished Got status: " + status);

            ConnectFinished?.Invoke(status);
        }

        private void OnDisconnectOrTimeout(ulong clientID)
        {
            if(clientID == MLAPI.NetworkManager.Singleton.LocalClientId )
            {
                //On a client disconnect we want to take them back to the main menu.
                //We have to check here in SceneManager if our active scene is the main menu, as if it is, it means we timed out rather than a raw disconnect;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                {
                    // we're not at the main menu, so we obviously had a connection before... thus, we aren't in a timeout scenario.
                    // Just shut down networking and switch back to main menu.
                    MLAPI.NetworkManager.Singleton.Shutdown();
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
        /// yet initialized its client and server GNP-Logic objects yet (which it does in NetworkStart, based on the role that the current player is performing).
        /// </remarks>
        /// <param name="portal"> </param>
        /// <param name="ipaddress">the IP address of the host to connect to. (currently IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public static void StartClient(GameNetPortal portal, string ipaddress, int port)
        {
            //DMW_NOTE: non-portable. We need to be updated when moving to UTP transport.
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

        private static void ConnectClient(GameNetPortal portal)
        {
            var clientGuid = ClientPrefs.GetGuid();
            //var payload = $"client_guid={clientGuid}\n"; //minimal format where key=value pairs are separated by newlines.
            //payload += $"client_scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex}\n";
            //payload += $"player_name={portal.PlayerName}\n";
			var payload = JsonUtility.ToJson(new ConnectionPayload() {clientGUID = clientGuid, clientScene = SceneManager.GetActiveScene().buildIndex, playerName = portal.PlayerName});

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            portal.NetManager.NetworkConfig.ConnectionData = payloadBytes;
            portal.NetManager.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

            //and...we're off! MLAPI will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by [???] (FIXME: GOMPS-79, need to handle transport layer failures too).
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported.
            portal.NetManager.StartClient();
        }
    }
}
