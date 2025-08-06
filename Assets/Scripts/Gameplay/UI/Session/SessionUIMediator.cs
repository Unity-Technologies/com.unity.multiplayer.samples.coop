using System;
using Unity.BossRoom.Gameplay.Configuration;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.UnityServices.Sessions;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class SessionUIMediator : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;
        [SerializeField]
        SessionJoiningUI m_SessionJoiningUI;
        [SerializeField]
        SessionCreationUI m_SessionCreationUI;
        [SerializeField]
        UITinter m_JoinToggleHighlight;
        [SerializeField]
        UITinter m_JoinToggleTabBlocker;
        [SerializeField]
        UITinter m_CreateToggleHighlight;
        [SerializeField]
        UITinter m_CreateToggleTabBlocker;
        [SerializeField]
        TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField]
        GameObject m_LoadingSpinner;

        AuthenticationServiceFacade m_AuthenticationServiceFacade;
        MultiplayerServicesFacade m_MultiplayerServicesFacade;
        LocalSessionUser m_LocalUser;
        LocalSession m_LocalSession;
        NameGenerationData m_NameGenerationData;
        ConnectionManager m_ConnectionManager;
        ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;

        const string k_DefaultSessionName = "no-name";
        const int k_MaxPlayers = 8;

        ISession m_Session;

        [Inject]
        void InjectDependenciesAndInitialize(
            AuthenticationServiceFacade authenticationServiceFacade,
            MultiplayerServicesFacade multiplayerServicesFacade,
            LocalSessionUser localUser,
            LocalSession localSession,
            NameGenerationData nameGenerationData,
            ISubscriber<ConnectStatus> connectStatusSub,
            ConnectionManager connectionManager
        )
        {
            m_AuthenticationServiceFacade = authenticationServiceFacade;
            m_NameGenerationData = nameGenerationData;
            m_LocalUser = localUser;
            m_MultiplayerServicesFacade = multiplayerServicesFacade;
            m_LocalSession = localSession;
            m_ConnectionManager = connectionManager;
            m_ConnectStatusSubscriber = connectStatusSub;
            RegenerateName();

            m_ConnectStatusSubscriber.Subscribe(OnConnectStatus);
        }

        void OnConnectStatus(ConnectStatus status)
        {
            if (status is ConnectStatus.GenericDisconnect or ConnectStatus.StartClientFailed)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnDestroy()
        {
            m_ConnectStatusSubscriber?.Unsubscribe(OnConnectStatus);
        }

        // Multiplayer Services SDK calls done from UI
        public async void CreateSessionRequest(string sessionName, bool isPrivate)
        {
            // before sending request, populate an empty session name, if necessary
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = k_DefaultSessionName;
            }

            BlockUIWhileLoadingIsInProgress();

            var playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            m_ConnectionManager.StartHostSession(m_LocalUser.DisplayName);

            var result = await m_MultiplayerServicesFacade.TryCreateSessionAsync(sessionName, k_MaxPlayers, isPrivate);

            HandleSessionJoinResult(result);
        }

        public async void QuerySessionRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            if (blockUI)
            {
                BlockUIWhileLoadingIsInProgress();
            }

            var playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (blockUI && !playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await m_MultiplayerServicesFacade.RetrieveAndPublishSessionListAsync();

            if (blockUI)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void JoinSessionWithCodeRequest(string sessionCode)
        {
            BlockUIWhileLoadingIsInProgress();

            var playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            m_ConnectionManager.StartClientSession(m_LocalUser.DisplayName);

            var result = await m_MultiplayerServicesFacade.TryJoinSessionByCodeAsync(sessionCode);

            HandleSessionJoinResult(result);
        }

        public async void JoinSessionRequest(ISessionInfo sessionInfo)
        {
            BlockUIWhileLoadingIsInProgress();

            var playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            m_ConnectionManager.StartClientSession(m_LocalUser.DisplayName);

            var result = await m_MultiplayerServicesFacade.TryJoinSessionByNameAsync(sessionInfo.Id);

            HandleSessionJoinResult(result);
        }

        public async void QuickJoinRequest()
        {
            BlockUIWhileLoadingIsInProgress();

            var playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            m_ConnectionManager.StartHostSession(m_LocalUser.DisplayName);

            var result = await m_MultiplayerServicesFacade.TryQuickJoinSessionAsync();

            HandleSessionJoinResult(result);
        }

        void HandleSessionJoinResult((bool Success, ISession Session) result)
        {
            if (result.Success)
            {
                OnJoinedSession(result.Session);
            }
            else
            {
                m_ConnectionManager.RequestShutdown();
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnJoinedSession(ISession remoteSession)
        {
            m_MultiplayerServicesFacade.SetRemoteSession(remoteSession);

            Debug.Log($"Joined session with ID: {m_LocalSession.SessionID}");

            m_ConnectionManager.StartClientSession(m_LocalUser.DisplayName);
        }

        //show/hide UI

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
            m_SessionCreationUI.Hide();
            m_SessionJoiningUI.Hide();
        }

        public void ToggleJoinSessionUI()
        {
            m_SessionJoiningUI.Show();
            m_SessionCreationUI.Hide();
            m_JoinToggleHighlight.SetToColor(1);
            m_JoinToggleTabBlocker.SetToColor(1);
            m_CreateToggleHighlight.SetToColor(0);
            m_CreateToggleTabBlocker.SetToColor(0);
        }

        public void ToggleCreateSessionUI()
        {
            m_SessionJoiningUI.Hide();
            m_SessionCreationUI.Show();
            m_JoinToggleHighlight.SetToColor(0);
            m_JoinToggleTabBlocker.SetToColor(0);
            m_CreateToggleHighlight.SetToColor(1);
            m_CreateToggleTabBlocker.SetToColor(1);
        }

        public void RegenerateName()
        {
            m_LocalUser.DisplayName = m_NameGenerationData.GenerateName();
            m_PlayerNameLabel.text = m_LocalUser.DisplayName;
        }

        void BlockUIWhileLoadingIsInProgress()
        {
            m_CanvasGroup.interactable = false;
            m_LoadingSpinner.SetActive(true);
        }

        void UnblockUIAfterLoadingIsComplete()
        {
            // this callback can happen after we've already switched to a different scene
            // in that case the canvas group would be null
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = true;
                m_LoadingSpinner.SetActive(false);
            }
        }
    }
}
