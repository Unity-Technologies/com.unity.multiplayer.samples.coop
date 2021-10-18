using System;
using Unity.Multiplayer.Samples.BossRoom.Client;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField]
        private PopupPanel m_ResponsePopup;

        private const string k_DefaultIP = "127.0.0.1";

        private GameNetPortal m_GameNetPortal;

        private Client.ClientGameNetPortal m_ClientNetPortal;

        private static MainMenuUI s_Instance = null;

        /// <summary>
        /// Small singleton getter, for easy access across other classes, this should be safe,
        /// as it is not touching any aspect of networking
        /// </summary>
        public static MainMenuUI Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<MainMenuUI>();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        private const int k_ConnectPort = 9998;

        void Start()
        {
            // Find the game Net Portal by tag - it should have been created by Startup
            GameObject GamePortalGO = GameObject.FindGameObjectWithTag("GameNetPortal");
            Assert.IsNotNull("No GameNetPortal found, Did you start the game from the Startup scene?");
            m_GameNetPortal = GamePortalGO.GetComponent<GameNetPortal>();
            m_ClientNetPortal = GamePortalGO.GetComponent<Client.ClientGameNetPortal>();

            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            m_ClientNetPortal.OnUnityRelayJoinFailed += OnRelayJoinFailed;
            m_ClientNetPortal.ConnectFinished += OnConnectFinished;

            //any disconnect reason set? Show it to the user here.
            ConnectStatusToMessage(m_ClientNetPortal.DisconnectReason.Reason, false);
            m_ClientNetPortal.DisconnectReason.Clear();
        }

        public void OnHostClicked()
        {
            m_ResponsePopup.SetupEnterGameDisplay(true, "Host Game", "Input the Host IP <br> or select another mode", "Select CONFIRM to host a Relay room <br> or select another mode", "Select CONFIRM to host a Unity Relay room <br> or select another mode", "iphost", "Confirm",
                (string connectInput, int connectPort, string playerName, OnlineMode onlineMode) =>
            {
                m_GameNetPortal.PlayerName = playerName;
                switch (onlineMode)
                {
                    case OnlineMode.Relay:
                        m_GameNetPortal.StartPhotonRelayHost(connectInput);
                        break;

                    case OnlineMode.IpHost:
                        m_GameNetPortal.StartHost(PostProcessIpInput(connectInput), connectPort);
                        break;

                    case OnlineMode.UnityRelay:
                        Debug.Log("Unity Relay Host clicked");
                        m_GameNetPortal.StartUnityRelayHost();
                        break;
                }
            }, k_DefaultIP, k_ConnectPort);
        }

        public void OnConnectClicked()
        {
            m_ResponsePopup.SetupEnterGameDisplay(false, "Join Game", "Input the host IP below", "Input the room name below", "Input the join code below", "iphost", "Join",
                (string connectInput, int connectPort, string playerName, OnlineMode onlineMode) =>
            {
                m_GameNetPortal.PlayerName = playerName;

                switch (onlineMode)
                {
                    case OnlineMode.Relay:
                        if (ClientGameNetPortal.StartClientRelayMode(m_GameNetPortal, connectInput, out string failMessage) == false)
                        {
                            m_ResponsePopup.SetupNotifierDisplay("Connection Failed", failMessage, false, true);
                            return;
                        }
                        break;

                    case OnlineMode.IpHost:
                        ClientGameNetPortal.StartClient(m_GameNetPortal, connectInput, connectPort);
                        break;

                    case OnlineMode.UnityRelay:
                        Debug.Log($"Unity Relay Client, join code {connectInput}");
                        m_ClientNetPortal.StartClientUnityRelayModeAsync(m_GameNetPortal, connectInput);
                        break;
                }
                m_ResponsePopup.SetupNotifierDisplay("Connecting", "Attempting to Join...", true, false);
            }, k_DefaultIP, k_ConnectPort);
        }

        private string PostProcessIpInput(string ipInput)
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
        private void OnConnectFinished(ConnectStatus status)
        {
            ConnectStatusToMessage(status, true);
        }

        /// <summary>
        /// Takes a ConnectStatus and shows an appropriate message to the user. This can be called on: (1) successful connect,
        /// (2) failed connect, (3) disconnect.
        /// </summary>
        /// <param name="connecting">pass true if this is being called in response to a connect finishing.</param>
        private void ConnectStatusToMessage(ConnectStatus status, bool connecting)
        {
            switch(status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "The Host is full and cannot accept any additional connections", false, true);
                    break;
                case ConnectStatus.Success:
                    if(connecting) { m_ResponsePopup.SetupNotifierDisplay("Success!", "Joining Now", false, true); }
                    break;
                case ConnectStatus.LoggedInAgain:
                    m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "You have logged in elsewhere using the same account", false, true);
                    break;
                case ConnectStatus.GenericDisconnect:
                    var title = connecting ? "Connection Failed" : "Disconnected From Host";
                    var text = connecting ? "Something went wrong" : "The connection to the host was lost";
                    m_ResponsePopup.SetupNotifierDisplay(title, text, false, true);
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }

        /// <summary>
        /// This should allow us to push a message pop up for connection responses from within other classes
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="displayImage"></param>
        /// <param name="displayConfirmation"></param>
        public void PushConnectionResponsePopup(string title, string message, bool displayImage, bool displayConfirmation)
        {
            m_ResponsePopup.SetupNotifierDisplay(title, message, displayImage, displayConfirmation);
        }

        /// <summary>
        /// Invoked when the client sent a connection request to the server and didn't hear back at all.
        /// This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        private void OnNetworkTimeout()
        {
            m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "Unable to Reach Host/Server", false, true, "Please try again");
        }

        private void OnRelayJoinFailed(string message)
        {
            PushConnectionResponsePopup("Unity Relay: Join Failed", $"{message}", true, true);
        }

        private void OnDestroy()
        {
            if (m_ClientNetPortal != null)
            {
                m_ClientNetPortal.NetworkTimedOut -= OnNetworkTimeout;
                m_ClientNetPortal.ConnectFinished -= OnConnectFinished;
                m_ClientNetPortal.OnUnityRelayJoinFailed -= OnRelayJoinFailed;
            }

            // Release this instance as soon as we are destroyed
            s_Instance = null;
        }
    }
}
