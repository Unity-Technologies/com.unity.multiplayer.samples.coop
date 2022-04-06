using System;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
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

        public async void CreateLobbyRequest(string lobbyName, bool isPrivate, int maxPlayers, OnlineMode onlineMode)
        {
            // before sending request to lobby service, populate an empty lobby name, if necessary
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = k_DefaultLobbyName;
            }

            BlockUIWhileLoadingIsInProgress();

            var lobbyCreationAttempt = await m_LobbyServiceFacade.TryCreateLobbyAsync(lobbyName, maxPlayers, isPrivate, onlineMode);

            if (lobbyCreationAttempt.Success)
            {
                m_LocalUser.IsHost = true;
                m_LobbyServiceFacade.SetRemoteLobby(lobbyCreationAttempt.Lobby);

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
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void QueryLobbiesRequest(bool blockUI)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            try
            {
                if (blockUI)
                {
                    BlockUIWhileLoadingIsInProgress();
                }

                await m_LobbyServiceFacade.RetrieveAndPublishLobbyListAsync();
                UnblockUIAfterLoadingIsComplete();
            }
            catch (Exception)
            {
                UnblockUIAfterLoadingIsComplete();
                throw;
            }
        }

        public async void JoinLobbyWithCodeRequest(string lobbyCode)
        {
            BlockUIWhileLoadingIsInProgress();

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

        void OnJoinedLobby(Lobby remoteLobby)
        {
            m_LobbyServiceFacade.SetRemoteLobby(remoteLobby);
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
