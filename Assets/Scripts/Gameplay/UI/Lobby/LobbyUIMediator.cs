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
    public class LobbyUIMediator : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] LobbyJoiningUI m_LobbyJoiningUI;
        [SerializeField] LobbyCreationUI m_LobbyCreationUI;
        [SerializeField] UITinter m_JoinToggleHighlight;
        [SerializeField] UITinter m_JoinToggleTabBlocker;
        [SerializeField] UITinter m_CreateToggleHighlight;
        [SerializeField] UITinter m_CreateToggleTabBlocker;
        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] GameObject m_LoadingSpinner;

        AuthenticationServiceFacade m_AuthenticationServiceFacade;
        MultiplayerServicesFacade m_MultiplayerServicesFacade;
        LocalSessionUser m_LocalUser;
        LocalSession m_LocalSession;
        NameGenerationData m_NameGenerationData;
        ConnectionManager m_ConnectionManager;
        ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;

        const string k_DefaultSessionName = "no-name";
        
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
            
            UnblockUIAfterLoadingIsComplete();
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
            
            var result = await m_MultiplayerServicesFacade.TryJoinSessionAsync(sessionCode, null);

            if (result.Success)
            {
                OnJoinedSession(result.Session);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
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
            
            var result = await m_MultiplayerServicesFacade.TryJoinSessionAsync(null, sessionInfo.Id);

            if (result.Success)
            {
                OnJoinedSession(result.Session);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
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

            if (result.Success)
            {
                OnJoinedSession(result.Session);
            }
            else
            {
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
            m_LobbyCreationUI.Hide();
            m_LobbyJoiningUI.Hide();
        }

        public void ToggleJoinLobbyUI()
        {
            m_LobbyJoiningUI.Show();
            m_LobbyCreationUI.Hide();
            m_JoinToggleHighlight.SetToColor(1);
            m_JoinToggleTabBlocker.SetToColor(1);
            m_CreateToggleHighlight.SetToColor(0);
            m_CreateToggleTabBlocker.SetToColor(0);
        }

        public void ToggleCreateLobbyUI()
        {
            m_LobbyJoiningUI.Hide();
            m_LobbyCreationUI.Show();
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
