using System;
using Unity.BossRoom.Gameplay.GameState;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the PostGame stage.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_PostGameUIDocument;
        
        [SerializeField]
        UIDocument m_MessageFeedDocument;

        ServerPostGameState m_PostGameState;
        
        Label m_WinEndMessage;
        Label m_LoseGameMessage;
        Button m_ReplayButton;
        Button m_WaitOnHostMsg;
        VisualElement m_MessageFeed;
        ListView m_MessageList;
        /*VisualElement m_SceneLight;
        Color m_WinLightColor;
        Color m_LoseLightColor;*/

        void Awake()
        {
            var root = m_PostGameUIDocument.rootVisualElement;
            var messageFeedRoot = m_MessageFeedDocument.rootVisualElement;
            m_WinEndMessage = root.Q<Label>("gameWinText");
            m_LoseGameMessage = root.Q<Label>("gameLostText");
            m_ReplayButton = root.Q<Button>("playAgainBtn");
            m_WaitOnHostMsg = root.Q<Button>("waitOnHostBtn");
            m_MessageFeed = messageFeedRoot.Q<VisualElement>("messageFeed");
            m_MessageList = m_MessageFeed.Q<ListView>("messageList");
            
            /*m_SceneLight = root.Q<VisualElement>("sceneLight");
            m_WinLightColor = root.Q<Color>("winLightColor");
            m_LoseLightColor = root.Q<Color>("loseLightColor");*/
            
            // Ensure labels are hidden at startup
            m_WinEndMessage.style.display = DisplayStyle.None;
            m_LoseGameMessage.style.display = DisplayStyle.None;
            
        }

        [Inject]
        void Inject(ServerPostGameState postGameState)
        {
            m_PostGameState = postGameState;

            // only hosts can restart the game, other players see a wait message
            if (NetworkManager.Singleton.IsHost)
            {
                m_ReplayButton.SetEnabled(true);
                m_WaitOnHostMsg.SetEnabled(false);
            }
            else
            {
                m_ReplayButton.SetEnabled(false);
                m_WaitOnHostMsg.SetEnabled(true);
            }
        }

        void Start()
        {
            m_PostGameState.NetworkPostGame.WinState.OnValueChanged += OnWinStateChanged;
            SetPostGameUI(m_PostGameState.NetworkPostGame.WinState.Value);
        }

        void OnDestroy()
        {
            if (m_PostGameState != null)
            {
                m_PostGameState.NetworkPostGame.WinState.OnValueChanged -= OnWinStateChanged;
            }
        }

        void OnWinStateChanged(WinState previousValue, WinState newValue)
        {
            SetPostGameUI(newValue);
        }

        void SetPostGameUI(WinState winState)
        {
            switch (winState)
            {
                // Set end message and background color based last game outcome
                case WinState.Win:
                   // m_SceneLight.color = m_WinLightColor;
                    m_WinEndMessage.SetEnabled(true);
                    m_LoseGameMessage.SetEnabled(false);
                    break;
                case WinState.Loss:
                    //m_SceneLight.color = m_LoseLightColor;
                    m_WinEndMessage.SetEnabled(false);
                    m_LoseGameMessage.SetEnabled(true);
                    break;
                case WinState.Invalid:
                    Debug.LogWarning("PostGameUI encountered Invalid WinState");
                    break;
            }
        }

        public void OnPlayAgainClicked()
        {
            m_PostGameState.PlayAgain();
        }

        public void OnMainMenuClicked()
        {
            m_PostGameState.GoToMainMenu();
        }
    }
}