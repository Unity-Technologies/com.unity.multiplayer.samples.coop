using System.Collections.Generic;
using System.Threading;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using GameLobby.UI;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace BossRoom.Scripts.Client.UI
{
    public class LobbyUIMediator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private JoinLobbyUI m_JoinLobbyUI;
        [SerializeField] private CreateLobbyUI m_CreateLobbyUI;
        [SerializeField] private UITinter m_JoinToggle;
        [SerializeField] private UITinter m_CreateToggle;
        [SerializeField] private TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] private GameObject m_LoadingSpinner;

        private LobbyAsyncRequests m_LobbyAsyncRequests;
        private LobbyUser m_localUser;
        private LocalLobby m_localLobby;
        private LobbyServiceData m_lobbyServiceData;
        private LobbyContentHeartbeat m_lobbyContentHeartbeat;
        private IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;
        private NameGenerationData m_NameGenerationData;
        private GameNetPortal m_GameNetPortal;
        private ClientGameNetPortal m_ClientNetPortal;

        [Inject]
        private void InjectDependencies(
            LobbyAsyncRequests lobbyAsyncRequests,
            IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher,
            LobbyUser localUser,
            LobbyContentHeartbeat lobbyContentHeartbeat,
            LobbyServiceData lobbyServiceData,
            LocalLobby localLobby,
            NameGenerationData nameGenerationData,
            GameNetPortal gameNetPortal,
            ClientGameNetPortal clientGameNetPortal
        )
        {
            //m_persistentPlayer = persistentPlayer;

            m_NameGenerationData = nameGenerationData;
            m_localUser = localUser;
            m_LobbyAsyncRequests = lobbyAsyncRequests;
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;
            m_lobbyContentHeartbeat = lobbyContentHeartbeat;
            m_lobbyServiceData = lobbyServiceData;
            m_localLobby = localLobby;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;

            RegenerateName();

            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            m_ClientNetPortal.ConnectFinished += OnConnectFinished;

            //any disconnect reason set? Show it to the user here.
            ConnectStatusToMessage(m_ClientNetPortal.DisconnectReason.Reason, false);
            m_ClientNetPortal.DisconnectReason.Clear();
        }

        private void OnDestroy()
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
            m_LobbyAsyncRequests.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, onlineMode, ip, port, OnCreatedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void QueryLobbiesRequest(bool blockUI)
        {

            m_LobbyAsyncRequests.RetrieveLobbyListAsync(
                OnSuccess,
                OnFailure
            );

            if (blockUI) BlockUIWhileLoadingIsInProgress();

            void OnSuccess(QueryResponse qr)
            {
                UnblockUIAfterLoadingIsComplete();
                var localLobbies = LocalLobby.CreateLocalLobbies(qr);

                var newLobbyDict = new Dictionary<string, LocalLobby>();

                foreach (var lobby in localLobbies)
                {
                    newLobbyDict.Add(lobby.LobbyID, lobby);
                }

                m_lobbyServiceData.FetchedLobbies(newLobbyDict);
            }

            void OnFailure()
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public void JoinLobbyWithCodeRequest(string lobbyCode)
        {
            m_LobbyAsyncRequests.JoinLobbyAsync(null, lobbyCode, OnJoinedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void JoinLobbyRequest(LocalLobby lobby)
        {
            m_LobbyAsyncRequests.JoinLobbyAsync(lobby.LobbyID, lobby.LobbyCode, OnJoinedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void QuickJoinRequest()
        {
            m_LobbyAsyncRequests.QuickJoinLobbyAsync(m_localUser, OnJoinedLobby, OnFailedLobbyCreateOrJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        private void OnFailedLobbyCreateOrJoin()
        {
            UnblockUIAfterLoadingIsComplete();
        }

        private void OnCreatedLobby(Lobby r)
        {
            m_localLobby.ApplyRemoteData(r);
            m_localUser.IsHost = true;

            m_LobbyAsyncRequests.BeginTracking(r);
            m_lobbyContentHeartbeat.BeginTracking();

            switch (m_localLobby.OnlineMode)
            {
                case OnlineMode.IpHost:
                    Debug.Log($"Created lobby with ID: {m_localLobby.LobbyID} and code {m_localLobby.LobbyCode}, at IP:Port {m_localLobby.Data.IP}:{m_localLobby.Data.Port}");
                    break;
                case OnlineMode.UnityRelay:
                    Debug.Log($"Created lobby with ID: {m_localLobby.LobbyID} and code {m_localLobby.LobbyCode}, Internal Relay Join Code{m_localLobby.RelayJoinCode}");
                    break;
            }

            m_GameNetPortal.PlayerName = m_localUser.DisplayName;

            var cancellationTokenSource = new CancellationTokenSource();

            switch (m_localLobby.OnlineMode)
            {
                case OnlineMode.IpHost:
                    m_GameNetPortal.StartHost(m_localLobby.Data.IP, m_localLobby.Data.Port);
                    break;

                case OnlineMode.UnityRelay:
                    m_GameNetPortal.StartUnityRelayHost(cancellationTokenSource.Token);
                    break;
            }
        }

        private void OnJoinedLobby(Lobby remoteLobby)
        {
            m_localLobby.ApplyRemoteData(remoteLobby);

             m_LobbyAsyncRequests.BeginTracking(remoteLobby);
             m_lobbyContentHeartbeat.BeginTracking();

            m_GameNetPortal.PlayerName = m_localUser.DisplayName;

            switch (m_localLobby.OnlineMode)
            {
                case OnlineMode.IpHost:
                    Debug.Log($"Joined lobby with code: {m_localLobby.LobbyCode}, at IP:Port {m_localLobby.Data.IP}:{m_localLobby.Data.Port}");
                    break;
                case OnlineMode.UnityRelay:
                    Debug.Log($"Joined lobby with code: {m_localLobby.LobbyCode}, Internal Relay Join Code{m_localLobby.RelayJoinCode}");
                    break;
            }


            var cancellationTokenSource = new CancellationTokenSource();

            switch (m_localLobby.OnlineMode)
            {
                case OnlineMode.IpHost:
                    m_ClientNetPortal.StartClient(m_GameNetPortal, m_localLobby.Data.IP, m_localLobby.Data.Port);
                    break;

                case OnlineMode.UnityRelay:
                    m_ClientNetPortal.StartClientUnityRelayModeAsync(m_GameNetPortal, m_localLobby.RelayJoinCode, cancellationTokenSource.Token, OnRelayJoinFailed);
                    break;
            }
        }

        private void OnRelayJoinFailed(string message)
        {
            Debug.Log($"Relay join failed: {message}");
            //leave the lobby if relay failed for some reason
            m_LobbyAsyncRequests.EndTracking();
            m_lobbyContentHeartbeat.EndTracking();

            if (!string.IsNullOrEmpty(m_localLobby?.LobbyID))
            {
                m_LobbyAsyncRequests.LeaveLobbyAsync(m_localLobby?.LobbyID, null, null);
            }

            m_localUser.ResetState();
            m_localLobby?.Reset(m_localUser);

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
            m_JoinLobbyUI.Show();
            m_CreateLobbyUI.Hide();
            m_JoinToggle.SetToColor(true);
            m_CreateToggle.SetToColor(false);
        }

        public void ToggleCreateLobbyUI()
        {
            m_JoinLobbyUI.Hide();
            m_CreateLobbyUI.Show();
            m_JoinToggle.SetToColor(false);
            m_CreateToggle.SetToColor(true);
        }

        public void RegenerateName()
        {
            m_localUser.DisplayName = m_NameGenerationData.GenerateName();
            m_PlayerNameLabel.text = m_localUser.DisplayName;
        }

        private void BlockUIWhileLoadingIsInProgress()
        {
            m_CanvasGroup.interactable = false;
            m_LoadingSpinner.SetActive(true);
        }

        private void UnblockUIAfterLoadingIsComplete()
        {
            m_CanvasGroup.interactable = true;
            m_LoadingSpinner.SetActive(false);
        }

        //m_clientNetPortal callbacks

        /// <summary>
        /// Callback when the server sends us back a connection finished event.
        /// </summary>
        /// <param name="status"></param>
        private void OnConnectFinished(ConnectStatus status)
        {
            ConnectStatusToMessage(status, true);
        }

        /// <summary>
        /// Invoked when the client sent a connection request to the server and didn't hear back at all.
        /// This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        private void OnNetworkTimeout()
        {
            UnblockUIAfterLoadingIsComplete();
            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Connection failed", "Unable to Reach Host/Server"));
        }


        /// <summary>
        ///     Takes a ConnectStatus and shows an appropriate message to the user. This can be called on: (1) successful connect,
        ///     (2) failed connect, (3) disconnect.
        /// </summary>
        /// <param name="connecting">pass true if this is being called in response to a connect finishing.</param>
        private void ConnectStatusToMessage(ConnectStatus status, bool connecting)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    Debug.Log($"{"Connection Failed"}, {"The Host is full and cannot accept any additional connections"}");
                    break;
                case ConnectStatus.Success:
                    if (connecting) Debug.Log($"{"Success!"}, {"Joining Now"}");

                    break;
                case ConnectStatus.LoggedInAgain:
                    Debug.Log($"{"Connection Failed"}, {"You have logged in elsewhere using the same account"}");
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
