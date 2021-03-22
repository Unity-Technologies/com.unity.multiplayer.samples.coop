using BossRoom.Client;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField]
        PopupPanel m_ResponsePopup;

        const string k_DefaultIP = "127.0.0.1";

        GameNetPortal m_GameNetPortal;

        ClientGameNetPortal m_ClientNetPortal;

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        const int k_ConnectPort = 9998;

        void Start()
        {
            // Find the game Net Portal by tag - it should have been created by Startup
            var gameNetPortalGameObject = GameObject.FindGameObjectWithTag("GameNetPortal");
            Assert.IsNotNull("No GameNetPortal found, Did you start the game from the Startup scene?");
            m_GameNetPortal = gameNetPortalGameObject.GetComponent<GameNetPortal>();
            m_ClientNetPortal = gameNetPortalGameObject.GetComponent<ClientGameNetPortal>();

            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            m_ClientNetPortal.ConnectFinished += OnConnectFinished;
        }

        public void OnHostClicked()
        {
            m_ResponsePopup.SetupEnterGameDisplay(true, "Host Game", "Input the IP to host on", "Input the room name to host on", "iphost", "Confirm",
                (string connectInput, string playerName, OnlineMode onlineMode) =>
            {
                m_GameNetPortal.PlayerName = playerName;
                switch (onlineMode)
                {
                    case OnlineMode.Relay:
                        m_GameNetPortal.StartRelayHost(connectInput);
                        break;

                    case OnlineMode.IpHost:
                        m_GameNetPortal.StartHost(PostProcessIpInput(connectInput), k_ConnectPort);
                        break;
                }
            }, k_DefaultIP);
        }

        public void OnConnectClicked()
        {
            m_ResponsePopup.SetupEnterGameDisplay(false, "Join Game", "Input the host IP below", "Input the room name below", "iphost", "Join",
                (string connectInput, string playerName, OnlineMode onlineMode) =>
            {
                m_GameNetPortal.PlayerName = playerName;

                switch (onlineMode)
                {
                    case OnlineMode.Relay:
                        ClientGameNetPortal.StartClientRelayMode(m_GameNetPortal, connectInput);
                        break;

                    case OnlineMode.IpHost:
                        ClientGameNetPortal.StartClient(m_GameNetPortal, connectInput, k_ConnectPort);
                        break;
                }
                m_ResponsePopup.SetupNotifierDisplay("Connecting", "Attempting to Join...", true, false);
            }, k_DefaultIP);
        }

        string PostProcessIpInput(string ipInput)
        {
            string ipAddress = ipInput;
            if (string.IsNullOrEmpty(ipInput))
            {
                ipAddress = k_DefaultIP;
            }

            return ipAddress;
        }

        /// <summary>
        /// Callback when the server sends us back a connection finished event.
        /// </summary>
        /// <param name="status"></param>
        void OnConnectFinished(ConnectStatus status)
        {
            if (status != ConnectStatus.Success)
            {
                m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "Something went wrong", false, true);
            }
            else
            {
                //Here we want to display that the connection was successful before we load in game.
                m_ResponsePopup.SetupNotifierDisplay("Success!", "Joining Now", false, false);
            }
        }

        /// <summary>
        /// Invoked when the client sent a connection request to the server and didn't hear back at all.
        /// This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        void OnNetworkTimeout()
        {
            m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "Unable to Reach Host/Server", false, true, "Please try again");
        }

        void OnDestroy()
        {
            m_ClientNetPortal.NetworkTimedOut -= OnNetworkTimeout;
            m_ClientNetPortal.ConnectFinished -= OnConnectFinished;
        }
    }
}
