using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    /// Handles the list of LobbyListItemUIs and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class LobbyJoiningUI : MonoBehaviour
    {
        [SerializeField] private LobbyListItemUI m_LobbyListItemPrototype;
        [SerializeField] private InputField m_JoinCodeField;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        private IInstanceResolver m_Container;

        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        private readonly Dictionary<string, LobbyListItemUI> m_LobbyListItemUIs = new Dictionary<string, LobbyListItemUI>();

        private LobbyUIMediator m_LobbyUIMediator;

        private readonly Dictionary<string, LocalLobby> m_LocalLobby = new Dictionary<string, LocalLobby>();

        private UpdateRunner m_UpdateRunner;

        private IDisposable m_Subscriptions;

        private void Awake()
        {
            m_LobbyListItemPrototype.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (m_UpdateRunner != null)
            {
                m_UpdateRunner.Unsubscribe(PeriodicRefresh);
            }
        }

        private void OnDestroy()
        {
            m_Subscriptions?.Dispose();
        }

        [Inject]
        private void InjectDependencies(IInstanceResolver container, LobbyUIMediator lobbyUIMediator, UpdateRunner updateRunner, ISubscriber<LocalLobbiesRefreshedMessage> localLobbiesRefreshedSub)
        {
            m_Container = container;
            m_LobbyUIMediator = lobbyUIMediator;
            m_UpdateRunner = updateRunner;

            m_UpdateRunner.Subscribe(PeriodicRefresh, 10f);

            m_Subscriptions = localLobbiesRefreshedSub.Subscribe(UpdateUI);
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

        private void UpdateUI(LocalLobbiesRefreshedMessage message)
        {
            //Check for new entries, We take CurrentLobbies as the source of truth
            var previousKeys = new List<string>(m_LobbyListItemUIs.Keys);

            foreach (var idToLobby in message.LobbyIDsToLocalLobbies)
            {
                var lobbyIDKey = idToLobby.Key;
                var lobbyData = idToLobby.Value;

                if (!m_LobbyListItemUIs.ContainsKey(lobbyIDKey))
                {
                    if (CanDisplay(lobbyData))
                    {
                        CreateLobbyListItem(lobbyIDKey, lobbyData);
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
        private void CreateLobbyListItem(string lobbyID, LocalLobby lobby)
        {
            var lobbyPanel = Instantiate(m_LobbyListItemPrototype.gameObject, m_LobbyListItemPrototype.transform.parent)
                .GetComponent<LobbyListItemUI>();
            lobbyPanel.gameObject.SetActive(true);
            m_Container.InjectIn(lobbyPanel);

            lobbyPanel.BeginObserving(lobby);
            lobby.onDestroyed += RemoveLobbyButton; // Set up to clean itself

            m_LobbyListItemUIs.Add(lobbyID, lobbyPanel);
            m_LocalLobby.Add(lobbyID, lobby);
        }

        private void UpdateLobbyButton(string lobbyCode, LocalLobby lobby)
        {
            m_LobbyListItemUIs[lobbyCode].UpdateLobby(lobby);
        }

        private void RemoveLobbyButton(LocalLobby lobby)
        {
            var lobbyID = lobby.LobbyID;
            var lobbyPanel = m_LobbyListItemUIs[lobbyID];
            lobbyPanel.EndObserving();
            m_LobbyListItemUIs.Remove(lobbyID);
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
