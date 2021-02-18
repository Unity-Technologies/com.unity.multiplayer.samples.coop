using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the MainMenu stage. 
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_GamePortalGO;

        [SerializeField]
        private PopupPanel m_ResponsePopup;

        //private const string k_DefaultIP = "127.0.0.1";

        //private GameNetPortal m_GameNetPortal;

        //private Client.ClientGameNetPortal m_ClientNetPortal;

        /// <summary>
        /// This will get more sophisticated as we move to a true relay model.
        /// </summary>
        private const int k_ConnectPort = 9998;

        void Start()
        {
           //m_GameNetPortal = m_GamePortalGO.GetComponent<GameNetPortal>();
            //m_ClientNetPortal = m_GamePortalGO.GetComponent<Client.ClientGameNetPortal>();

            //m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            //m_ClientNetPortal.ConnectFinished += OnConnectFinished;
        }

        public void OnPlayAgainClicked()
        {


        }

        public void OnMainMenuClicked()
        {

        }
    }
}

