using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage. 
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject GameHubGO;

        public PopupPanel m_ResponsePopup;

        private const string k_DefaultIP = "127.0.0.1";

        private BossRoom.GameNetPortal m_netHub;

        private Client.ClientGameNetPortal m_ClientNetPortal;

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        private const int k_ConnectPort = 9998;

        // Start is called before the first frame update
        void Start()
        {
            m_netHub = GameHubGO.GetComponent<BossRoom.GameNetPortal>();
            m_ClientNetPortal = GameHubGO.GetComponent<Client.ClientGameNetPortal>();

            m_ClientNetPortal.NetworkTimeOutEvent += OnNetworkTimeout;
            m_ClientNetPortal.onConnectFinished += OnConnectFinished;
        }

        public void OnHostClicked()
        {

            m_ResponsePopup.SetupInputDisplay("Host Game", "Input the IP to host on", "iphost", "Confirm", (string input) =>
            {
                string ipAddress = input;
                if (input == "")
                {
                    ipAddress = k_DefaultIP;
                }

                m_netHub.StartHost(ipAddress, k_ConnectPort);
            }, k_DefaultIP);
        }

        public void OnConnectClicked()
        {
            m_ResponsePopup.SetupInputDisplay("Join Game", "Input the host IP below", "iphost", "Join", (string input) =>
            {
                string ipAddress = input;
                if (input == "")
                {
                    ipAddress = k_DefaultIP;
                }

                BossRoom.Client.ClientGameNetPortal.StartClient(m_netHub, ipAddress, k_ConnectPort);
                m_ResponsePopup.SetupNotifierDisplay("Connecting", "Attempting to Join...", true, false);

            }, k_DefaultIP);
        }

        /// <summary>
        /// Callback when the server sends us back a connection finished event.
        /// </summary>
        /// <param name="status"></param>
        private void OnConnectFinished(ConnectStatus status)
        {
            if (status != ConnectStatus.SUCCESS)
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
        private void OnNetworkTimeout()
        {
            m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "Unable to Reach Host/Server", false, true, "Please try again");
        }

        private void OnDestroy()
        {
            m_ClientNetPortal.NetworkTimeOutEvent -= OnNetworkTimeout;
            m_ClientNetPortal.onConnectFinished -= OnConnectFinished;
        }
    }
}

