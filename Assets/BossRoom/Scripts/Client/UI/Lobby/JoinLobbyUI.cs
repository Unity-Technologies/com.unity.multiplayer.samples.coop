using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    /// Handles the list of LobbyPanelUIs and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class JoinLobbyUI : ObserverBehaviour<LobbyServiceData>
    {
        [SerializeField] private LobbyPanelUI m_LobbyPanelPrototype;
        [SerializeField] private InputField m_JoinCodeField;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        private IInstanceResolver m_Container;

        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        private readonly Dictionary<string, LobbyPanelUI> m_LobbyButtons = new Dictionary<string, LobbyPanelUI>();

        private LobbyServiceData m_LobbyServiceData;
        private LobbyUIMediator m_LobbyUIMediator;

        private readonly Dictionary<string, LocalLobby> m_LocalLobby = new Dictionary<string, LocalLobby>();

        private UpdateRunner m_UpdateRunner;

        private void Awake()
        {
            m_LobbyPanelPrototype.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (m_UpdateRunner != null)
            {
                m_UpdateRunner.Unsubscribe(PeriodicRefresh);
            }
        }

        [Inject]
        private void InjectDependencies(IInstanceResolver container, LobbyUIMediator lobbyUIMediator, LobbyServiceData lobbyServiceData, UpdateRunner updateRunner)
        {
            m_Container = container;
            m_LobbyUIMediator = lobbyUIMediator;
            m_LobbyServiceData = lobbyServiceData;
            m_UpdateRunner = updateRunner;

            m_UpdateRunner.Subscribe(PeriodicRefresh, 10f);
            BeginObserving(m_LobbyServiceData);
        }

        public void OnJoinButtonPressed()
        {
            m_LobbyUIMediator.JoinLobbyWithCodeRequest(m_JoinCodeField.text.ToUpper());
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

            //Check for new entries, We take CurrentLobbies as the source of truth
            var previousKeys = new List<string>(m_LobbyButtons.Keys);

            foreach (var idToLobby in observed.CurrentLobbies)
            {
                var lobbyIDKey = idToLobby.Key;
                var lobbyData = idToLobby.Value;

                if (!m_LobbyButtons.ContainsKey(lobbyIDKey))
                {
                    if (CanDisplay(lobbyData))
                    {
                        CreateLobbyPanel(lobbyIDKey, lobbyData);
                    }
                }
                else
                {
                    if (CanDisplay(lobbyData))
                    {
                        UpdateLobbyButton(lobbyIDKey, lobbyData);
                    }
                    else
                    {
                        RemoveLobbyButton(lobbyData);
                    }
                }

                previousKeys.Remove(lobbyIDKey);
            }

            //remove the lobbies that no longer exist
            foreach (var key in previousKeys)
            {
                RemoveLobbyButton(m_LocalLobby[key]);
            }
        }

        public void OnQuickJoinClicked()
        {
            m_LobbyUIMediator.QuickJoinRequest();
        }

        private bool CanDisplay(LocalLobby lobby)
        {
            return lobby.LobbyUsers.Count != lobby.MaxPlayerCount;
        }

        /// <summary>
        /// Instantiates UI element and initializes the observer with the LobbyData
        /// </summary>
        private void CreateLobbyPanel(string lobbyID, LocalLobby lobby)
        {
            var lobbyPanel = Instantiate(m_LobbyPanelPrototype.gameObject, m_LobbyPanelPrototype.transform.parent)
                .GetComponent<LobbyPanelUI>();
            lobbyPanel.gameObject.SetActive(true);
            m_Container.InjectIn(lobbyPanel);

            lobbyPanel.BeginObserving(lobby);
            lobby.onDestroyed += RemoveLobbyButton; // Set up to clean itself

            m_LobbyButtons.Add(lobbyID, lobbyPanel);
            m_LocalLobby.Add(lobbyID, lobby);
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
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;

            m_JoinCodeField.text = "";
            OnRefresh();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}
