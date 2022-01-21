using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using TMPro;
using UnityEngine;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    /// Handles the list of LobbyButtons and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class JoinLobbyUI : ObserverBehaviour<LobbyServiceData>
    {
        private UIFactory m_UIFactory;
        private LobbyUIManager m_LobbyUIManager;
        private LobbyServiceData m_LobbyServiceData;

        [SerializeField] private LobbyPanelUI m_LobbyPanelPrototype;

        //
        [SerializeField]
        TMP_InputField m_JoinCodeField;

        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        Dictionary<string, LobbyPanelUI> m_LobbyButtons = new Dictionary<string, LobbyPanelUI>();
        Dictionary<string, LocalLobby> m_LocalLobby = new Dictionary<string, LocalLobby>();

        /// <summary>Contains some amount of information used to join an existing lobby.</summary>
        LocalLobby.LobbyData m_LocalLobbySelected;


        [Inject]
        private void InjectDependencies(UIFactory uiFactory, LobbyUIManager lobbyUIManager, LobbyServiceData lobbyServiceData)
        {
            m_UIFactory = uiFactory;
            m_LobbyUIManager = lobbyUIManager;
            m_LobbyServiceData = lobbyServiceData;
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
            m_LobbyUIManager.JoinLobbyRequest(m_LocalLobbySelected);
            m_LocalLobbySelected = default;
        }

        public void OnRefresh()
        {
            m_LobbyUIManager.QueryLobbiesRequest();
        }

        protected override void UpdateObserver(LobbyServiceData observed)
        {
            ///Check for new entries, We take CurrentLobbies as the source of truth
            var previousKeys = new List<string>(m_LobbyButtons.Keys);

            foreach (var codeLobby in observed.CurrentLobbies)
            {
                var lobbyCodeKey = codeLobby.Key;
                var lobbyData = codeLobby.Value;

                if (!m_LobbyButtons.ContainsKey(lobbyCodeKey))
                {
                    if (CanDisplay(lobbyData))
                    {
                        CreateLobbyPanel(lobbyCodeKey, lobbyData);
                    }
                }
                else
                {
                    if (CanDisplay(lobbyData))
                    {
                        UpdateLobbyButton(lobbyCodeKey, lobbyData);
                    }
                    else
                    {
                        RemoveLobbyButton(lobbyData);
                    }
                }

                previousKeys.Remove(lobbyCodeKey);
            }

            foreach (var key in previousKeys)
            {
                // Need to remove any lobbies from the list that no longer exist.
                RemoveLobbyButton(m_LocalLobby[key]);
            }
        }

        public void JoinMenuChangedVisibility(bool show)
        {
            if (show)
            {
                m_JoinCodeField.text = "";
                OnRefresh();
            }
        }

        public void OnQuickJoinClicked()
        {
            m_LobbyUIManager.QuickJoinRequest();
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
            var lobbyPanel = m_UIFactory
                .InstantiateActive(m_LobbyPanelPrototype.gameObject, m_LobbyPanelPrototype.transform.parent)
                .GetComponent<LobbyPanelUI>();

            lobbyPanel.BeginObserving(lobby);
            lobbyPanel.OnClicked.AddListener(LobbyPanelSelected);
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
    }
}
