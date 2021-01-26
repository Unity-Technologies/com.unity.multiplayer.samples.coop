using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage. 
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject GameHubGO;
        public GameObject InputTextGO;

        public PopupPanelManager m_ResponsePopup;

        private const string k_DefaultIP = "127.0.0.1";

        private BossRoom.GameNetPortal m_netHub;

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        private const int k_ConnectPort = 9998;

        // Start is called before the first frame update
        void Start()
        {
            m_netHub = GameHubGO.GetComponent<BossRoom.GameNetPortal>();
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

                Debug.Log(ipAddress);

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
                //Immediately after this change the display to show the
                m_ResponsePopup.SetupNotifierDisplay("Connecting", "Attempting to Join...", true, false);

            }, k_DefaultIP);
        }

        /// <summary>
        /// Callback when the server sends us back 
        /// </summary>
        /// <param name="status"></param>
        public void OnConnectFinished(ConnectStatus status)
        {
            Debug.Log("HI");
            if (status != ConnectStatus.SUCCESS)
            {
                
                m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "Something went wrong", false, true);
            }
            else
            {
               //Here we want to display that the connection was successful.
            }
        }
    }
}

