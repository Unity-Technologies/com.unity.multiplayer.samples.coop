using System;
using Unity.BossRoom.Gameplay.Configuration;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Services.Core;
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
        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobbyUser m_LocalUser;
        LocalLobby m_LocalLobby;
        NameGenerationData m_NameGenerationData;
        ConnectionManager m_ConnectionManager;
        ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;

        const string k_DefaultLobbyName = "no-name";

        [Inject]
        void InjectDependenciesAndInitialize(
            AuthenticationServiceFacade authenticationServiceFacade,
            LobbyServiceFacade lobbyServiceFacade,
            LocalLobbyUser localUser,
            LocalLobby localLobby,
            NameGenerationData nameGenerationData,
            ISubscriber<ConnectStatus> connectStatusSub,
            ConnectionManager connectionManager
        )
        {
            m_AuthenticationServiceFacade = authenticationServiceFacade;
            m_NameGenerationData = nameGenerationData;
            m_LocalUser = localUser;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
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
            if (m_ConnectStatusSubscriber != null)
            {
                m_ConnectStatusSubscriber.Unsubscribe(OnConnectStatus);
            }
        }

        //Lobby and Relay calls done from UI

        public async void CreateLobbyRequest(string lobbyName, bool isPrivate)
        {
            // before sending request to lobby service, populate an empty lobby name, if necessary
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = k_DefaultLobbyName;
            }

            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var lobbyCreationAttempt = await m_LobbyServiceFacade.TryCreateLobbyAsync(lobbyName, m_ConnectionManager.MaxConnectedPlayers, isPrivate);

            if (lobbyCreationAttempt.Success)
            {
                m_LocalUser.IsHost = true;
                m_LobbyServiceFacade.SetRemoteLobby(lobbyCreationAttempt.Lobby);

                Debug.Log($"Created lobby with ID: {m_LocalLobby.LobbyID} and code {m_LocalLobby.LobbyCode}");
                m_ConnectionManager.StartHostLobby(m_LocalUser.DisplayName);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void QueryLobbiesRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            if (blockUI)
            {
                BlockUIWhileLoadingIsInProgress();
            }

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (blockUI && !playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await m_LobbyServiceFacade.RetrieveAndPublishLobbyListAsync();

            if (blockUI)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void JoinLobbyWithCodeRequest(string lobbyCode)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var result = await m_LobbyServiceFacade.TryJoinLobbyAsync(null, lobbyCode);

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void JoinLobbyRequest(LocalLobby lobby)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var result = await m_LobbyServiceFacade.TryJoinLobbyAsync(lobby.LobbyID, lobby.LobbyCode);

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void QuickJoinRequest()
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var result = await m_LobbyServiceFacade.TryQuickJoinLobbyAsync();

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnJoinedLobby(Unity.Services.Lobbies.Models.Lobby remoteLobby)
        {
            m_LobbyServiceFacade.SetRemoteLobby(remoteLobby);

            Debug.Log($"Joined lobby with code: {m_LocalLobby.LobbyCode}, Internal Relay Join Code{m_LocalLobby.RelayJoinCode}");
            m_ConnectionManager.StartClientLobby(m_LocalUser.DisplayName);
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
            //this callback can happen after we've already switched to a different scene
            //in that case the canvas group would be null
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = true;
                m_LoadingSpinner.SetActive(false);
            }
        }
    }
}
