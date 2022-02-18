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

        private LobbyUIMediator m_LobbyUIMediator;

        private UpdateRunner m_UpdateRunner;

        private IDisposable m_Subscriptions;

        private List<LobbyListItemUI> m_LobbyListItems = new List<LobbyListItemUI>();

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
        private void InjectDependenciesAndInstantiate(
            IInstanceResolver container,
            LobbyUIMediator lobbyUIMediator,
            UpdateRunner updateRunner,
            ISubscriber<LobbyListFetchedMessage> localLobbiesRefreshedSub)
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

        private void UpdateUI(LobbyListFetchedMessage message)
        {
            var displayableLobbies = GetDisplayableLobbies(message.LocalLobbies);

            EnsureNumberOfActiveUISlots(displayableLobbies.Count);

            for (var i = 0; i < displayableLobbies.Count; i++)
            {
                var localLobby = message.LocalLobbies[i];
                m_LobbyListItems[i].SetData(localLobby);
            }
        }

        private List<LocalLobby> GetDisplayableLobbies(IReadOnlyList<LocalLobby> lobbies)
        {
            var displayable = new List<LocalLobby>();

            foreach (var lobby in lobbies)
            {
                if (CanDisplay(lobby))
                {
                    displayable.Add(lobby);
                }
            }

            return displayable;
        }

        private void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - m_LobbyListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                m_LobbyListItems.Add(CreateLobbyListItem());
            }

            for (int i = requiredNumber; i < m_LobbyListItems.Count; i++)
            {
                m_LobbyListItems[i].gameObject.SetActive(false);
            }
        }


        private LobbyListItemUI CreateLobbyListItem()
        {
            var listItem = Instantiate(m_LobbyListItemPrototype.gameObject, m_LobbyListItemPrototype.transform.parent)
                .GetComponent<LobbyListItemUI>();
            listItem.gameObject.SetActive(true);
            m_Container.InjectIn(listItem);
            return listItem;
        }

        public void OnQuickJoinClicked()
        {
            m_LobbyUIMediator.QuickJoinRequest();
        }

        private bool CanDisplay(LocalLobby lobby)
        {
            return lobby.LobbyUsers.Count != lobby.MaxPlayerCount;
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
