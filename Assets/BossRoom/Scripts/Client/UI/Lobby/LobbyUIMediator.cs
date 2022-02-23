using System.Collections.Generic;
using GameLobby.UI;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
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
        [SerializeField] UITinter m_CreateToggle;
        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] GameObject m_LoadingSpinner;

        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobbyUser m_LocalUser;
        LocalLobby m_LocalLobby;
        IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;
        NameGenerationData m_NameGenerationData;
        GameNetPortal m_GameNetPortal;
        ClientGameNetPortal m_ClientNetPortal;

        [Inject]
        void InjectDependenciesAndInitialize(
            LobbyServiceFacade lobbyServiceFacade,
            IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher,
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
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;
            m_LocalLobby = localLobby;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;

            RegenerateName();

            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            m_ClientNetPortal.ConnectFinished += OnConnectFinished;

            //any disconnect reason set? Show it to the user here.
            ConnectStatusToMessage(m_ClientNetPortal.DisconnectReason.Reason, false);
            m_ClientNetPortal.DisconnectReason.Clear();
        }

        void OnDestroy()
        {
            if (m_ClientNetPortal != null)
            {
                m_ClientNetPortal.NetworkTimedOut -= OnNetworkTimeout;
                m_ClientNetPortal.ConnectFinished -= OnConnectFinished;
            }
        }

        //Lobby and Relay calls done from UI

        public void CreateLobbyRequest(string lobbyName, bool isPrivate, int maxPlayers, OnlineMode onlineMode, string ip, int port)
        {
            m_LobbyServiceFacade.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, onlineMode, ip, port, OnCreatedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void QueryLobbiesRequest(bool blockUI)
        {
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

        void OnCreatedLobby(Lobby r)
        {
            m_LocalUser.IsHost = true;
            m_LobbyServiceFacade.BeginTracking(r);

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
        }

        void OnRelayJoinFailed(string message)
        {
            Debug.Log($"Relay join failed: {message}");
            //leave the lobby if relay failed for some reason
            m_LobbyServiceFacade.EndTracking();

            UnblockUIAfterLoadingIsComplete();
            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Unity Relay: Join Failed", message));
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
            m_CreateToggle.SetToColor(0);
        }

        public void ToggleCreateLobbyUI()
        {
            m_LobbyJoiningUI.Hide();
            m_LobbyCreationUI.Show();
            m_JoinToggle.SetToColor(0);
            m_CreateToggle.SetToColor(1);
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
        /// Callback when the server sends us back a connection finished event.
        /// </summary>
        /// <param name="status"></param>
        void OnConnectFinished(ConnectStatus status)
        {
            ConnectStatusToMessage(status, true);
        }

        /// <summary>
        /// Invoked when the client sent a connection request to the server and didn't hear back at all.
        /// This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        void OnNetworkTimeout()
        {
            UnblockUIAfterLoadingIsComplete();
            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Connection failed", "Unable to Reach Host/Server"));
        }

        /// <summary>
        /// Takes a ConnectStatus and shows an appropriate message to the user. This can be called on: (1) successful connect,
        /// (2) failed connect, (3) disconnect.
        /// </summary>
        /// <param name="connecting">pass true if this is being called in response to a connect finishing.</param>
        void ConnectStatusToMessage(ConnectStatus status, bool connecting)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    Debug.Log("Connection Failed, The Host is full and cannot accept any additional connections");
                    break;
                case ConnectStatus.Success:
                    if (connecting)
                    {
                        Debug.Log("Success!, Joining Now");
                    }
                    break;
                case ConnectStatus.LoggedInAgain:
                    Debug.Log("Connection Failed, You have logged in elsewhere using the same account");
                    break;
                case ConnectStatus.GenericDisconnect:
                    var title = connecting ? "Connection Failed" : "Disconnected From Host";
                    var text = connecting ? "Something went wrong" : "The connection to the host was lost";
                    Debug.Log($"{title}, {text}");
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }
    }
}
