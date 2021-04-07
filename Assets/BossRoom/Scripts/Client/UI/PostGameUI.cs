using MLAPI.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the PostGame stage.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField]
        private Image m_EndMessage;

        [SerializeField]
        private Light m_SceneLight;
		
        [SerializeField]
        private TextMeshProUGUI m_WinEndMessage;

        [SerializeField]
        private TextMeshProUGUI m_LoseGameMessage;

        [SerializeField]
        private GameObject m_ReplayButton;

        [SerializeField]
        private GameObject m_WaitOnHostMsg;

        [SerializeField]
        private PostGameData m_PostGameData;

        [SerializeField]
        private Color m_WinLightColor;

        [SerializeField]
        private Color m_LoseLightColor;

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

            OnGameWonChanged(0, m_PostGameData.GameBannerState.Value );
            m_PostGameData.GameBannerState.OnValueChanged += OnGameWonChanged;
        }

        //this won't actually change dynamically, but using a callback robustifies us against race
        //conditions between the PostGameState starting up, and this UI starting up.
        private void OnGameWonChanged(byte prevVal, byte currentVal)
        {
            // Set end message and background color based last game outcome
            if (m_PostGameData.GameBannerState.Value == (byte)PostGameData.BannerState.Won )
            {
                m_SceneLight.color = m_WinLightColor;
                m_WinEndMessage.gameObject.SetActive(true);
                m_LoseGameMessage.gameObject.SetActive(false);
            }
            else if( m_PostGameData.GameBannerState.Value == (byte)PostGameData.BannerState.Lost )
            {
                m_SceneLight.color = m_LoseLightColor;
                m_WinEndMessage.gameObject.SetActive(false);
                m_LoseGameMessage.gameObject.SetActive(true);
            }
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
            MLAPI.NetworkManager.Singleton.StopClient();
            MLAPI.NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }
    }
}

