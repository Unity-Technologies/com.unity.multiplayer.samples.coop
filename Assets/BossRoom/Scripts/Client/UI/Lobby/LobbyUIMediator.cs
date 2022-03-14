using System;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class LobbyUIMediator : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] LobbyJoiningUI m_LobbyJoiningUI;
        [SerializeField] LobbyCreationUI m_LobbyCreationUI;
        [SerializeField] UITinter m_JoinToggle;
        [SerializeField] UITinter m_JoinToggleHighlight;
        [SerializeField] UITinter m_JoinToggleTabBlocker;
        [SerializeField] UITinter m_CreateToggle;
        [SerializeField] UITinter m_CreateToggleHighlight;
        [SerializeField] UITinter m_CreateToggleTabBlocker;
        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] GameObject m_LoadingSpinner;

        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobbyUser m_LocalUser;
        LocalLobby m_LocalLobby;
        NameGenerationData m_NameGenerationData;
        GameNetPortal m_GameNetPortal;
        ClientGameNetPortal m_ClientNetPortal;

        const string k_DefaultLobbyName = "no-name";

        [Inject]
        void InjectDependenciesAndInitialize(
            LobbyServiceFacade lobbyServiceFacade,
            LocalLobbyUser localUser,
            LocalLobby localLobby,
            NameGenerationData nameGenerationData,
            GameNetPortal gameNetPortal,
            ClientGameNetPortal clientGameNetPortal
        )
        {
            m_NameGenerationData = nameGenerationData;
            m_LocalUser = localUser;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;

            RegenerateName();

            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
        }

        void OnDestroy()
        {
            if (m_ClientNetPortal != null)
            {
                m_ClientNetPortal.NetworkTimedOut -= OnNetworkTimeout;
            }
        }

        //Lobby and Relay calls done from UI

        public void CreateLobbyRequest(string lobbyName, bool isPrivate, int maxPlayers, OnlineMode onlineMode)
        {
            // before sending request to lobby service, populate an empty lobby name, if necessary
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = k_DefaultLobbyName;
            }

            m_LobbyServiceFacade.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, onlineMode, OnCreatedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void QueryLobbiesRequest(bool blockUI)
        {
            if (!AuthenticationService.Instance.IsAuthorized)
            {
                return;
            }

            m_LobbyServiceFacade.RetrieveLobbyListAsync(
                OnSuccess,
                OnFailure
            );

            if (blockUI)
            {
                BlockUIWhileLoadingIsInProgress();
            }

            void OnSuccess(QueryResponse qr)
            {
                UnblockUIAfterLoadingIsComplete();
            }

            void OnFailure()
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public void JoinLobbyWithCodeRequest(string lobbyCode)
        {
            m_LobbyServiceFacade.JoinLobbyAsync(null, lobbyCode, OnJoinedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void JoinLobbyRequest(LocalLobby lobby)
        {
            m_LobbyServiceFacade.JoinLobbyAsync(lobby.LobbyID, lobby.LobbyCode, OnJoinedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void QuickJoinRequest()
        {
            m_LobbyServiceFacade.QuickJoinLobbyAsync(OnJoinedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        void OnFailedLobbyCreateOrJoin()
        {
            UnblockUIAfterLoadingIsComplete();
        }

        void OnCreatedLobby(Lobby lobby)
        {
            m_LocalUser.IsHost = true;
            m_LobbyServiceFacade.BeginTracking(lobby);

            m_GameNetPortal.PlayerName = m_LocalUser.DisplayName;

            switch (m_LocalLobby.OnlineMode)
            {
                case OnlineMode.IpHost:
                    Debug.Log($"Created lobby with ID: {m_LocalLobby.LobbyID} and code {m_LocalLobby.LobbyCode}, at IP:Port {m_LocalLobby.Data.IP}:{m_LocalLobby.Data.Port}");
                    m_GameNetPortal.StartHost(m_LocalLobby.Data.IP, m_LocalLobby.Data.Port);
                    break;

                case OnlineMode.UnityRelay:
                    Debug.Log($"Created lobby with ID: {m_LocalLobby.LobbyID} and code {m_LocalLobby.LobbyCode}, Internal Relay Join Code{m_LocalLobby.RelayJoinCode}");
                    m_GameNetPortal.StartUnityRelayHost();
                    break;
            }
        }

        void OnJoinedLobby(Lobby remoteLobby)
        {
            m_LobbyServiceFacade.BeginTracking(remoteLobby);
            m_GameNetPortal.PlayerName = m_LocalUser.DisplayName;

            switch (m_LocalLobby.OnlineMode)
            {
                case OnlineMode.IpHost:
                    Debug.Log($"Joined lobby with code: {m_LocalLobby.LobbyCode}, at IP:Port {m_LocalLobby.Data.IP}:{m_LocalLobby.Data.Port}");
                    m_ClientNetPortal.StartClient(m_LocalLobby.Data.IP, m_LocalLobby.Data.Port);
                    break;

                case OnlineMode.UnityRelay:
                    Debug.Log($"Joined lobby with code: {m_LocalLobby.LobbyCode}, Internal Relay Join Code{m_LocalLobby.RelayJoinCode}");
                    m_ClientNetPortal.StartClientUnityRelayModeAsync(m_LocalLobby.RelayJoinCode, OnRelayJoinFailed);
                    break;
            }

            void OnRelayJoinFailed(string message)
            {
                PopupPanel.ShowPopupPanel("Relay join failed", message);
                Debug.Log($"Relay join failed: {message}");
                //leave the lobby if relay failed for some reason
                m_LobbyServiceFacade.EndTracking();
                UnblockUIAfterLoadingIsComplete();
            }
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
        }

        public void ToggleJoinLobbyUI()
        {
            m_LobbyJoiningUI.Show();
            m_LobbyCreationUI.Hide();
            m_JoinToggle.SetToColor(1);
            m_JoinToggleHighlight.SetToColor(1);
            m_JoinToggleTabBlocker.SetToColor(1);
            m_CreateToggle.SetToColor(0);
            m_CreateToggleHighlight.SetToColor(0);
            m_CreateToggleTabBlocker.SetToColor(0);
        }

        public void ToggleCreateLobbyUI()
        {
            m_LobbyJoiningUI.Hide();
            m_LobbyCreationUI.Show();
            m_JoinToggle.SetToColor(0);
            m_JoinToggleHighlight.SetToColor(0);
            m_JoinToggleTabBlocker.SetToColor(0);
            m_CreateToggle.SetToColor(1);
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

        /// <summary>
        /// Invoked when the client sent a connection request to the server and didn't hear back at all.
        /// This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        void OnNetworkTimeout()
        {
            m_LobbyServiceFacade.EndTracking();
            UnblockUIAfterLoadingIsComplete();
        }
    }
}
