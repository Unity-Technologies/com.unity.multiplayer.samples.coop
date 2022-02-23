using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the list of LobbyListItemUIs and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class LobbyJoiningUI : MonoBehaviour
    {
        [SerializeField] LobbyListItemUI m_LobbyListItemPrototype;
        [SerializeField] InputField m_JoinCodeField;
        [SerializeField] CanvasGroup m_CanvasGroup;

        IInstanceResolver m_Container;

        LobbyUIMediator m_LobbyUIMediator;

        UpdateRunner m_UpdateRunner;

        IDisposable m_Subscriptions;

        List<LobbyListItemUI> m_LobbyListItems = new List<LobbyListItemUI>();

        void Awake()
        {
            m_LobbyListItemPrototype.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (m_UpdateRunner != null)
            {
                m_UpdateRunner.Unsubscribe(PeriodicRefresh);
            }
        }

        void OnDestroy()
        {
            m_Subscriptions?.Dispose();
        }

        [Inject]
        void InjectDependenciesAndInstantiate(
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

        void PeriodicRefresh(float _)
        {
            //this is a soft refresh without needing to lock the UI and such
            m_LobbyUIMediator.QueryLobbiesRequest(false);
        }

        public void OnRefresh()
        {
            m_LobbyUIMediator.QueryLobbiesRequest(true);
        }

        void UpdateUI(LobbyListFetchedMessage message)
        {
            var displayableLobbies = GetDisplayableLobbies(message.LocalLobbies);

            EnsureNumberOfActiveUISlots(displayableLobbies.Count);

            for (var i = 0; i < displayableLobbies.Count; i++)
            {
                var localLobby = message.LocalLobbies[i];
                m_LobbyListItems[i].SetData(localLobby);
            }
        }

        List<LocalLobby> GetDisplayableLobbies(IReadOnlyList<LocalLobby> lobbies)
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

        void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - m_LobbyListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                m_LobbyListItems.Add(CreateLobbyListItem());
            }

            for (int i = 0; i < m_LobbyListItems.Count; i++)
            {
                m_LobbyListItems[i].gameObject.SetActive( i < requiredNumber );
            }
        }

        LobbyListItemUI CreateLobbyListItem()
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

        bool CanDisplay(LocalLobby lobby)
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
