using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoomViz
{
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject GameHubGO;
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

        public void OnHostClicked()
        {
            Debug.Log("Host Clicked");

            //TODO: bring up transition screen. 

            m_netHub.NetManager.StartHost();
        }

        public void OnConnectClicked()
        {
            Debug.Log("Connect Clicked");
            m_netHub.StartClient("127.0.0.1", 7777);
        }
    }
}

