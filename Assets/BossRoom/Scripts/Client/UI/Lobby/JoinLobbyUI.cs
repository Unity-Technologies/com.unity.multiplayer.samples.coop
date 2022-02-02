using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    /// Handles the list of LobbyButtons and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class JoinLobbyUI : ObserverBehaviour<LobbyServiceData>
    {
        private GameObjectFactory m_GameObjectFactory;
        private LobbyUIMediator m_LobbyUIMediator;
        private LobbyServiceData m_LobbyServiceData;
        private UpdateRunner m_UpdateRunner;

        [SerializeField] private LobbyPanelUI m_LobbyPanelPrototype;

        //
        [SerializeField] private InputField m_JoinCodeField;

        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        private Dictionary<string, LobbyPanelUI> m_LobbyButtons = new Dictionary<string, LobbyPanelUI>();

        private Dictionary<string, LocalLobby> m_LocalLobby = new Dictionary<string, LocalLobby>();

        /// <summary>Contains some amount of information used to join an existing lobby.</summary>
        private LocalLobby.LobbyData m_LocalLobbySelected;


        [SerializeField] private CanvasGroup m_CanvasGroup;

        [Inject]
        private void InjectDependencies(GameObjectFactory gameObjectFactory, LobbyUIMediator lobbyUIMediator, LobbyServiceData lobbyServiceData, UpdateRunner updateRunner)
        {
            m_GameObjectFactory = gameObjectFactory;
            m_LobbyUIMediator = lobbyUIMediator;
            m_LobbyServiceData = lobbyServiceData;
            m_UpdateRunner = updateRunner;

            m_UpdateRunner.Subscribe(PeriodicRefresh, 10f);
            BeginObserving(m_LobbyServiceData);
        }

        private void OnDisable()
        {
            if (m_UpdateRunner != null) m_UpdateRunner.Unsubscribe(PeriodicRefresh);
        }

        private void Awake()
        {
            m_LobbyPanelPrototype.gameObject.SetActive(false);
        }

        public void LobbyPanelSelected(LocalLobby lobby)
        {
            m_LocalLobbySelected = lobby.Data;
        }

        public void OnLobbyCodeInputFieldChanged(string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
                m_LocalLobbySelected = new LocalLobby.LobbyData(newCode.ToUpper());
        }

        public void OnJoinButtonPressed()
        {
            m_LobbyUIMediator.JoinLobbyRequest(m_LocalLobbySelected);
            m_LocalLobbySelected = default;
        }

        private void PeriodicRefresh(float _)
        {
            m_LobbyUIMediator.QueryLobbiesRequest(false);
        }


        public void OnRefresh()
        {
            m_LobbyUIMediator.QueryLobbiesRequest(true);
        }

        protected override void UpdateObserver(LobbyServiceData observed)
        {
            base.UpdateObserver(observed);

            ///Check for new entries, We take CurrentLobbies as the source of truth
            var previousKeys = new List<string>(m_LobbyButtons.Keys);

            foreach (var codeLobby in observed.CurrentLobbies)
            {
                var lobbyCodeKey = codeLobby.Key;
                var lobbyData = codeLobby.Value;

                if (!m_LobbyButtons.ContainsKey(lobbyCodeKey))
                {
                    if (CanDisplay(lobbyData)) CreateLobbyPanel(lobbyCodeKey, lobbyData);
                }
                else
                {
                    if (CanDisplay(lobbyData))
                        UpdateLobbyButton(lobbyCodeKey, lobbyData);
                    else
                        RemoveLobbyButton(lobbyData);
                }

                previousKeys.Remove(lobbyCodeKey);
            }

            foreach (var key in previousKeys)
                // Need to remove any lobbies from the list that no longer exist.
                RemoveLobbyButton(m_LocalLobby[key]);
        }

        public void OnQuickJoinClicked()
        {
            m_LobbyUIMediator.QuickJoinRequest();
        }

        private bool CanDisplay(LocalLobby lobby)
        {
            return lobby.Data.State == LobbyState.Lobby && !lobby.Private;
        }

        /// <summary>
        /// Instantiates UI element and initializes the observer with the LobbyData
        /// </summary>
        private void CreateLobbyPanel(string lobbyCode, LocalLobby lobby)
        {
            var lobbyPanel = m_GameObjectFactory
                .InstantiateActiveAndInjected(m_LobbyPanelPrototype.gameObject, m_LobbyPanelPrototype.transform.parent)
                .GetComponent<LobbyPanelUI>();

            lobbyPanel.BeginObserving(lobby);
            lobby.onDestroyed += RemoveLobbyButton; // Set up to clean itself

            m_LobbyButtons.Add(lobbyCode, lobbyPanel);
            m_LocalLobby.Add(lobbyCode, lobby);
        }

        private void UpdateLobbyButton(string lobbyCode, LocalLobby lobby)
        {
            m_LobbyButtons[lobbyCode].UpdateLobby(lobby);
        }

        private void RemoveLobbyButton(LocalLobby lobby)
        {
            var lobbyID = lobby.LobbyID;
            var lobbyPanel = m_LobbyButtons[lobbyID];
            lobbyPanel.EndObserving();
            m_LobbyButtons.Remove(lobbyID);
            m_LocalLobby.Remove(lobbyID);
            //todo: reuse lobby panel UIs instead of creating new ones
            Destroy(lobbyPanel.gameObject);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;

            m_JoinCodeField.text = "";
            OnRefresh();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}
