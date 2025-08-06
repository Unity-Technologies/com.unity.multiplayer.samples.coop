using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices.Sessions;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Handles the list of SessionListItemUIs and ensures it stays synchronized with the Session list from the service.
    /// </summary>
    public class SessionJoiningUI : MonoBehaviour
    {
        [SerializeField]
        SessionListItemUI m_SessionListItemPrototype;
        [SerializeField]
        InputField m_JoinCodeField;
        [SerializeField]
        CanvasGroup m_CanvasGroup;
        [SerializeField]
        Graphic m_EmptySessionListLabel;
        [SerializeField]
        Button m_JoinSessionButton;

        IObjectResolver m_Container;
        SessionUIMediator m_SessionUIMediator;
        UpdateRunner m_UpdateRunner;
        ISubscriber<SessionListFetchedMessage> m_LocalSessionsRefreshedSub;

        List<SessionListItemUI> m_SessionListItems = new List<SessionListItemUI>();

        void Awake()
        {
            m_SessionListItemPrototype.gameObject.SetActive(false);
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
            if (m_LocalSessionsRefreshedSub != null)
            {
                m_LocalSessionsRefreshedSub.Unsubscribe(UpdateUI);
            }
        }

        [Inject]
        void InjectDependenciesAndInitialize(
            IObjectResolver container,
            SessionUIMediator sessionUIMediator,
            UpdateRunner updateRunner,
            ISubscriber<SessionListFetchedMessage> localSessionsRefreshedSub)
        {
            m_Container = container;
            m_SessionUIMediator = sessionUIMediator;
            m_UpdateRunner = updateRunner;
            m_LocalSessionsRefreshedSub = localSessionsRefreshedSub;
            m_LocalSessionsRefreshedSub.Subscribe(UpdateUI);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the join code text.
        /// </summary>
        public void OnJoinCodeInputTextChanged()
        {
            m_JoinCodeField.text = SanitizeJoinCode(m_JoinCodeField.text);
            m_JoinSessionButton.interactable = m_JoinCodeField.text.Length > 0;
        }

        string SanitizeJoinCode(string dirtyString)
        {
            return Regex.Replace(dirtyString.ToUpper(), "[^A-Z0-9]", "");
        }

        public void OnJoinButtonPressed()
        {
            m_SessionUIMediator.JoinSessionWithCodeRequest(SanitizeJoinCode(m_JoinCodeField.text));
        }

        void PeriodicRefresh(float _)
        {
            //this is a soft refresh without needing to lock the UI and such
            m_SessionUIMediator.QuerySessionRequest(false);
        }

        public void OnRefresh()
        {
            m_SessionUIMediator.QuerySessionRequest(true);
        }

        void UpdateUI(SessionListFetchedMessage message)
        {
            EnsureNumberOfActiveUISlots(message.LocalSessions.Count);

            for (var i = 0; i < message.LocalSessions.Count; i++)
            {
                var localSession = message.LocalSessions[i];
                m_SessionListItems[i].SetData(localSession);
            }

            if (message.LocalSessions.Count == 0)
            {
                m_EmptySessionListLabel.enabled = true;
            }
            else
            {
                m_EmptySessionListLabel.enabled = false;
            }
        }

        void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - m_SessionListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                m_SessionListItems.Add(CreateSessionListItem());
            }

            for (int i = 0; i < m_SessionListItems.Count; i++)
            {
                m_SessionListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        SessionListItemUI CreateSessionListItem()
        {
            var listItem = Instantiate(m_SessionListItemPrototype.gameObject, m_SessionListItemPrototype.transform.parent)
                .GetComponent<SessionListItemUI>();
            listItem.gameObject.SetActive(true);

            m_Container.Inject(listItem);

            return listItem;
        }

        public void OnQuickJoinClicked()
        {
            m_SessionUIMediator.QuickJoinRequest();
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            m_JoinCodeField.text = "";
            m_UpdateRunner.Subscribe(PeriodicRefresh, 10f);
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
            m_UpdateRunner.Unsubscribe(PeriodicRefresh);
        }
    }
}
