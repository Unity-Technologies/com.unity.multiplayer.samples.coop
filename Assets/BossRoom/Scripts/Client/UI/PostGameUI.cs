using MLAPI.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the PostGame stage. 
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField]
        private RawImage m_Background;

        [SerializeField]
        private Image m_EndMessage;

        [SerializeField]
        private GameObject m_ReplayButton;

        [SerializeField]
        private GameObject m_WaitOnHostMsg;

        [SerializeField]
        private Sprite m_WinSprite;
        [SerializeField]
        private Sprite m_LoseSprite;

        void Start()
        {
            GameObject GamePortalGO = GameObject.FindGameObjectWithTag("GameNetPortal");
            var netPortal = GamePortalGO.GetComponent<GameNetPortal>();

            // only hosts can restart the game, other players see a wait message 
            if (netPortal.NetManager.IsHost)
            {
                m_ReplayButton.SetActive(true);
                m_WaitOnHostMsg.SetActive(false);
            }
            else
            {
                m_ReplayButton.SetActive(false);
                m_WaitOnHostMsg.SetActive(true);
            }

            // Set end message and background color based last game outcome
            if (netPortal.LastGameWon.Value)
            {
                m_EndMessage.sprite = m_WinSprite;
                m_Background.color = Color.white;
            }
            else
            {
                m_EndMessage.sprite = m_LoseSprite;
                m_Background.color = new Color(1.0f, 0.5f, 0.5f); 
            }
            // finally set message color to white to make it visible
            m_EndMessage.color = Color.white;
        }

        public void OnPlayAgainClicked()
        {
            // this should only ever be called by the Host - so just go ahead and switch scenes
            NetworkSceneManager.SwitchScene("CharSelect");

            // FUTURE: could be improved to better support a dedicated server architecture
        }

        public void OnMainMenuClicked()
        {
            // Player is leaving this group - leave current network connection first
            MLAPI.NetworkingManager.Singleton.StopClient();
            MLAPI.NetworkingManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }
    }
}

