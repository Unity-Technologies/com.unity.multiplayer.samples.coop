using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Viz
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage. 
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject GameHubGO;
        public GameObject InputTextGO;

        private BossRoom.GameNetHub m_netHub;

        // Start is called before the first frame update
        void Start()
        {
            m_netHub = GameHubGO.GetComponent<BossRoom.GameNetHub>();
        }

        // Update is called once per frame
        void Update()
        {

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

            m_netHub.StartHost(GetIPAddress(), 9998);
        }

        public void OnConnectClicked()
        {
            m_netHub.StartClient(GetIPAddress(), 9998);
        }
    }
}

