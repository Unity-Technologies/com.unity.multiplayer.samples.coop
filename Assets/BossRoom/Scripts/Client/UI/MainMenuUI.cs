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

        private BossRoom.GameNetPortal m_netHub;

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        private const int k_connectPort = 9998;

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
                    ipAddress = "127.0.0.1";
                }

                Debug.Log(ipAddress);

                m_netHub.StartHost(ipAddress, k_connectPort);
            });
        }

        public void OnConnectClicked()
        {
            m_ResponsePopup.SetupInputDisplay("Join Game", "Input the host IP below", "iphost", "Join", (string input) =>
            {
                string ipAddress = input;
                if (input == "")
                {
                    ipAddress = "127.0.0.1";
                }

                BossRoom.Client.ClientGameNetPortal.StartClient(m_netHub, ipAddress, k_connectPort);
            });
        }
    }
}

