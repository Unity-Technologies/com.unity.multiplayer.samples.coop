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
        RawImage m_Background;

        [SerializeField]
        Image m_EndMessage;

        [SerializeField]
        GameObject m_ReplayButton;

        [SerializeField]
        GameObject m_WaitOnHostMsg;

        [SerializeField]
        PostGameData m_PostGameData;

        [SerializeField]
        Sprite m_WinSprite;
        [SerializeField]
        Sprite m_LoseSprite;

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
        void OnGameWonChanged(byte prevVal, byte currentVal)
        {
            // Set end message and background color based last game outcome
            if (m_PostGameData.GameBannerState.Value == (byte)PostGameData.BannerState.Won )
            {
                m_EndMessage.sprite = m_WinSprite;
                m_Background.color = Color.white;
                m_EndMessage.color = Color.white;
            }
            else if( m_PostGameData.GameBannerState.Value == (byte)PostGameData.BannerState.Lost )
            {
                m_EndMessage.sprite = m_LoseSprite;
                m_Background.color = new Color(1.0f, 0.5f, 0.5f);
                m_EndMessage.color = Color.white;
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

