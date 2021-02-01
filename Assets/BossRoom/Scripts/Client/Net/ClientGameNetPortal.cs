using System;
using UnityEngine;
using MLAPI;
using MLAPI.Transports.UNET;

namespace BossRoom.Client
{
    /// <summary>
    /// Client logic for the GameNetHub. Contains implementations for all of GameNetHub's S2C RPCs.
    /// </summary>
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour
    {
        private GameNetPortal m_Hub;

        public void Start()
        {
            m_Hub = GetComponent<GameNetPortal>();
            m_Hub.NetworkStartEvent += this.NetworkStart;
            m_Hub.ConnectFinishedEvent += this.OnConnectFinished;
        }

        public void NetworkStart()
        {
            if( !m_Hub.NetManager.IsClient ) { this.enabled = false; }
        }


        public void OnConnectFinished( ConnectStatus status )
        {
            //on success, there is nothing to do (the MLAPI scene management system will take us to the next scene).
            //on failure, we must raise an event so that the UI layer can display something.
            Debug.Log("RecvConnectFinished Got status: " + status);
        }


        /// <summary>
        /// Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it.
        /// </summary>
        /// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
        private static string GetOrCreateGuid()
        {
            if( PlayerPrefs.HasKey("client_guid"))
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
            var clientGUID = GetOrCreateGuid();
            var payload = $"client_guid={clientGUID}\n"; //minimal format where key=value pairs are separated by newlines.

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            //DMW_NOTE: non-portable. We need to be updated when moving to UTP transport.
            var chosenTransport = NetworkingManager.Singleton.NetworkConfig.NetworkTransport;
            switch (chosenTransport)
            {
                case LiteNetLibTransport.LiteNetLibTransport liteNetLibTransport:
                    liteNetLibTransport = hub.NetworkingManagerGO.GetComponent<LiteNetLibTransport.LiteNetLibTransport>();
                    liteNetLibTransport.Address = ipaddress;
                    liteNetLibTransport.Port = (ushort)port;
                    break;
                case UnetTransport unetTransport:
                    unetTransport = hub.NetworkingManagerGO.GetComponent<UnetTransport>();
                    unetTransport.ConnectAddress = ipaddress;
                    unetTransport.ConnectPort = port;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(chosenTransport));
            }

            hub.NetManager.NetworkConfig.ConnectionData = payloadBytes;

            //and...we're off! MLAPI will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by [???] (FIXME: GOMPS-79, need to handle transport layer failures too).
            //  If the socket connection succeeds, we'll get our RecvConnectFinished invoked. This is where game-layer failures will be reported.
            hub.NetManager.StartClient();
        }

    }
}
