using System.Collections;
using System.Collections.Generic;
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

        private BossRoom.GameNetHub m_netHub;

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        private const int k_connectPort = 9998;

        // Start is called before the first frame update
        void Start()
        {
            m_netHub = GameHubGO.GetComponent<BossRoom.GameNetHub>();
        }

        /// <summary>
        /// Gets the IP Address the user set in the UI, or returns 127.0.0.1 if IP is not present. 
        /// </summary>
        /// <returns>IP address entered by user, in string form. </returns>
        private string GetIPAddress()
        {
            string iptext = InputTextGO.GetComponent<UnityEngine.UI.Text>().text;
            if( iptext == "" )
            {
                return "127.0.0.1";
            }

            return iptext;
        }

        public void OnHostClicked()
        {
            GetIPAddress();

            m_netHub.StartHost(GetIPAddress(), k_connectPort);
        }

        public void OnConnectClicked()
        {
            BossRoom.Client.ClientGNHLogic.StartClient(m_netHub, GetIPAddress(), k_connectPort);
        }
    }
}

