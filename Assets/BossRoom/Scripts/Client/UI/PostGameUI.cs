using MLAPI;
using MLAPI.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the PostGame stage.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField]
        Light m_SceneLight;

        [SerializeField]
        TextMeshProUGUI m_WinEndMessage;

        [SerializeField]
        TextMeshProUGUI m_LoseGameMessage;

        [SerializeField]
        GameObject m_ReplayButton;

        [SerializeField]
        GameObject m_WaitOnHostMsg;

        [SerializeField]
        GameObject m_PostGameData;

        NetworkWinState m_NetworkWinState;

        [SerializeField]
        Color m_WinLightColor;

        [SerializeField]
        Color m_LoseLightColor;

        void Start()
        {
            // only hosts can restart the game, other players see a wait message
            if (NetworkManager.Singleton.IsHost)
            {
                m_ReplayButton.SetActive(true);
                m_WaitOnHostMsg.SetActive(false);
            }
            else
            {
                m_ReplayButton.SetActive(false);
                m_WaitOnHostMsg.SetActive(true);
            }

            if (m_PostGameData.TryGetComponent(out m_NetworkWinState))
            {
                OnGameWonChanged(0, m_NetworkWinState.NetworkWin);
                m_NetworkWinState.AddListener(OnGameWonChanged);
            }
        }

        void OnDestroy()
        {
            if (m_NetworkWinState != null)
            {
                m_NetworkWinState.RemoveListener(OnGameWonChanged);
            }
        }

        //this won't actually change dynamically, but using a callback robustifies us against race
        //conditions between the PostGameState starting up, and this UI starting up.
        void OnGameWonChanged(WinState previousValue, WinState newValue)
        {
            // Set end message and background color based last game outcome
            if (newValue == WinState.Win)
            {
                m_SceneLight.color = m_WinLightColor;
                m_WinEndMessage.gameObject.SetActive(true);
                m_LoseGameMessage.gameObject.SetActive(false);
            }
            else if (newValue == WinState.Loss)
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
            var gameNetPortal = GameObject.FindGameObjectWithTag("GameNetPortal").GetComponent<GameNetPortal>();
            gameNetPortal.RequestDisconnect();

            SceneManager.LoadScene("MainMenu");
        }
    }
}

