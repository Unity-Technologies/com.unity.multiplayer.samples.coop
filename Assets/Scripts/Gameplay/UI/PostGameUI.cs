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

        ServerPostGameState m_PostGameState;

        Label m_WinEndMessage;
        Label m_LoseGameMessage;
        Button m_ReplayButton;
        Button m_WaitOnHostButton;
        Button m_MainMenuButton;

        VisualElement m_MessageFeed;
        ListView m_MessageList;

        void Awake()
        {
            var root = m_PostGameUIDocument.rootVisualElement;

            m_WinEndMessage = root.Q<Label>("gameWinText");
            m_LoseGameMessage = root.Q<Label>("gameLostText");
            m_ReplayButton = root.Q<Button>("playAgainBtn");
            m_WaitOnHostButton = root.Q<Button>("waitOnHostBtn");
            m_MainMenuButton = root.Q<Button>("menuBtn");

            m_WaitOnHostButton.SetEnabled(false);
            m_ReplayButton.SetEnabled(true);
            m_MainMenuButton.SetEnabled(true);

            m_ReplayButton.clicked += OnPlayAgainClicked;
            m_MainMenuButton.clicked += OnMainMenuClicked;
        }

        [Inject]
        void Inject(ServerPostGameState postGameState)
        {
            m_PostGameState = postGameState;
        }

        public void Initialize(bool isHost)
        {
            // only hosts can restart the game, other players see a wait message
            if (isHost)
            {
                m_ReplayButton.style.display = DisplayStyle.Flex;
                m_WaitOnHostButton.style.display = DisplayStyle.None;
                m_MainMenuButton.style.display = DisplayStyle.Flex;
            }

            else
            {
                m_ReplayButton.style.display = DisplayStyle.None;
                m_WaitOnHostButton.style.display = DisplayStyle.Flex;
                m_MainMenuButton.style.display = DisplayStyle.Flex;
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
                case WinState.Win:
                    m_WinEndMessage.style.display = DisplayStyle.Flex;
                    m_LoseGameMessage.style.display = DisplayStyle.None;
                    break;
                case WinState.Loss:
                    m_WinEndMessage.style.display = DisplayStyle.None;
                    m_LoseGameMessage.style.display = DisplayStyle.Flex;
                    break;
                case WinState.Invalid:
                    Debug.LogWarning("PostGameUI encountered Invalid WinState");
                    m_WinEndMessage.style.display = DisplayStyle.None;
                    m_LoseGameMessage.style.display = DisplayStyle.None;
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
