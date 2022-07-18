using System;
using UnityEngine;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the PostGame stage.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
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
        private Color m_WinLightColor;

        [SerializeField]
        private Color m_LoseLightColor;


        ConnectionManager m_ConnectionManager;
        ISubscriber<WinStateMessage> m_WinStateSub;

        IDisposable m_WinStateSubscription;

        [Inject]
        void Inject(ISubscriber<WinStateMessage> winStateSub, ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
            m_WinStateSub = winStateSub;

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

            m_WinStateSubscription = m_WinStateSub.Subscribe(SetPostGameUI);
        }

        void SetPostGameUI(WinStateMessage winStateMessage)
        {
            m_WinStateSubscription.Dispose();

            switch (winStateMessage.WinState)
            {
                // Set end message and background color based last game outcome
                case WinState.Win:
                    m_SceneLight.color = m_WinLightColor;
                    m_WinEndMessage.gameObject.SetActive(true);
                    m_LoseGameMessage.gameObject.SetActive(false);
                    break;
                case WinState.Loss:
                    m_SceneLight.color = m_LoseLightColor;
                    m_WinEndMessage.gameObject.SetActive(false);
                    m_LoseGameMessage.gameObject.SetActive(true);
                    break;
                case WinState.Invalid:
                    Debug.LogError("PostGameUI encountered Invalid WinState");
                    break;
            }
        }

        public void OnPlayAgainClicked()
        {
            // this should only ever be called by the Host - so just go ahead and switch scenes
            SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);

            // FUTURE: could be improved to better support a dedicated server architecture
        }

        public void OnMainMenuClicked()
        {
            m_ConnectionManager.RequestShutdown();
        }
    }
}

