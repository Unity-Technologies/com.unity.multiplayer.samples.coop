using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoomViz
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage. 
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject GameHubGO;
        private BossRoom.GameNetHub m_netHub;

        private string m_defaultIPText;

        // Start is called before the first frame update
        void Start()
        {
            m_netHub = GameHubGO.GetComponent<BossRoom.GameNetHub>();

            //track what this is on startup. We're not opinionated; we will try whatever the user puts in, as long as they've changed the text.
            m_defaultIPText = GameObject.Find("IPInputBox").GetComponent<UnityEngine.UI.Text>().text;
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
            string iptext = GameObject.Find("IPInputText").GetComponent<UnityEngine.UI.Text>().text;
            if( iptext == m_defaultIPText )
            {
                return "127.0.0.1";
            }

            return iptext;
        }

        public void OnHostClicked()
        {
            Debug.Log("Host Clicked");

            GetIPAddress();

            m_netHub.StartHost(GetIPAddress(), 9998);
        }

        public void OnConnectClicked()
        {
            Debug.Log("Connect Clicked");
            m_netHub.StartClient(GetIPAddress(), 9998);
        }
    }
}

