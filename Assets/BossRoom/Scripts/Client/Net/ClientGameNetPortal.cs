using System;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Client logic for the GameNetHub. Contains implementations for all of GameNetHub's S2C RPCs. 
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour
    {
        private GameNetPortal m_Hub;

        /// <summary>
        /// Time in seconds before the client considers a lack of server response a timeout
        /// </summary>
        private const int k_TimeoutDuration = 10;

        public event Action<ConnectStatus> onConnectFinished;


        /// <summary>
        /// This event fires when the client sent out a request to start the client, but failed to hear back after an allotted amount of
        /// time from the host.  
        /// </summary>
        public event Action NetworkTimeOutEvent;


        public void Start()
        {
            m_Hub = GetComponent<GameNetPortal>();
            m_Hub.networkStartEvent += this.NetworkStart;
            m_Hub.ConnectFinishedEvent += this.OnConnectFinished;
            m_Hub.NetManager.OnClientDisconnectCallback += OnDisconnectOrTimeout;
        }

        public void NetworkStart()
        {
            if (!m_Hub.NetManager.IsClient) { this.enabled = false; }
        }


        public void OnConnectFinished(ConnectStatus status)
        {
            //on success, there is nothing to do (the MLAPI scene management system will take us to the next scene). 
            //on failure, we must raise an event so that the UI layer can display something. 
            Debug.Log("RecvConnectFinished Got status: " + status);

            onConnectFinished?.Invoke(status);
        }

        private void OnDisconnectOrTimeout(ulong clientID)
        {

            //On a client disconnect we want to take them back to the main menu.
            //We have to check here in SceneManager if our active scene is the main menu, as if it is, it means we timed out rather than a raw disconnect;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
            {
                //FIXME:  Currently it is not possible to safely load back to the Main Menu scene due to Persisting objects getting recreated
                //We still don't want to invoke the network timeout event, however.
            }
            else
            {
                NetworkTimeOutEvent?.Invoke();
            }
        }


        /// <summary>
        /// Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it. 
        /// </summary>
        /// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
        private static string GetOrCreateGuid()
        {
            if (PlayerPrefs.HasKey("client_guid"))
            {
                return PlayerPrefs.GetString("client_guid");
            }

            System.Guid g = System.Guid.NewGuid();
            string guid_string = g.ToString();

            PlayerPrefs.SetString("client_guid", guid_string);
            return guid_string;
        }

        /// <summary>
        /// Wraps the invocation of NetworkingManager.StartClient, including our GUID as the payload. 
        /// </summary>
        /// <remarks>
        /// This method must be static because, when it is invoked, the client still doesn't know it's a client yet, and in particular, GameNetHub hasn't
        /// yet initialized its client and server GNHLogic objects yet (which it does in NetworkStart, based on the role that the current player is performing). 
        /// </remarks>
        /// <param name="ipaddress">the IP address of the host to connect to. (currently IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public static void StartClient(GameNetPortal hub, string ipaddress, int port)
        {
            string client_guid = GetOrCreateGuid();
            string payload = $"client_guid={client_guid}\n"; //minimal format where key=value pairs are separated by newlines. 

            byte[] payload_bytes = System.Text.Encoding.UTF8.GetBytes(payload);

            //DMW_NOTE: non-portable. We need to be updated when moving to UTP transport. 
            var transport = hub.NetworkingManagerGO.GetComponent<MLAPI.Transports.UNET.UnetTransport>();
            transport.ConnectAddress = ipaddress;
            transport.ConnectPort = port;

            hub.NetManager.NetworkConfig.ConnectionData = payload_bytes;
            hub.NetManager.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

            //and...we're off! MLAPI will establish a socket connection to the host. 
            //  If the socket connection fails, we'll hear back by [???] (FIXME: GOMPS-79, need to handle transport layer failures too).
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported. 
            hub.NetManager.StartClient();
        }

    }
}
