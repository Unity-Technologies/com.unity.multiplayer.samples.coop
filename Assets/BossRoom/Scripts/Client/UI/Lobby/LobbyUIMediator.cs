using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using GameLobby.UI;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using GameState = BossRoom.Scripts.Shared.Net.UnityServices.Lobbies.GameState;

namespace BossRoom.Scripts.Client.UI
{
    public class LobbyUIMediator : MonoBehaviour
    {
        #region Fields

        //Injected dependencies
        private IInstanceResolver _container;
        private LobbyAsyncRequests m_LobbyAsyncRequests;
        private LobbyUser m_localUser;
        private LocalLobby m_localLobby;
        private LobbyServiceData m_lobbyServiceData;
        private LobbyContentHeartbeat m_lobbyContentHeartbeat;
        private IPublisher<UnityServiceErrorMessage> m_DisplayErrorPopupPublisher;
        private IPublisher<UserStatus> m_LobbyUserStatusPublisher;
        private Identity m_Identity;
        private LocalGameState m_localGameState;
        private NameGenerationData m_NameGenerationData;
        private LocalLobbyFactory m_LocalLobbyFactory;
        private GameNetPortal m_GameNetPortal;
        private ClientGameNetPortal m_ClientNetPortal;

        private IDisposable m_DisposableSubscriptions;

        //Inspector fields
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private JoinLobbyUI m_JoinLobbyUI;
        [SerializeField] private CreateLobbyUI m_CreateLobbyUI;
        [SerializeField] private UITinter m_JoinToggle;
        [SerializeField] private UITinter m_CreateToggle;
        [SerializeField] private TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] private GameObject m_LoadingSpinner;

        #endregion

        #region Lifetime

        [Inject]
        private void InjectDependencies(
            LobbyAsyncRequests lobbyAsyncRequests,
            IPublisher<UnityServiceErrorMessage> displayErrorPopupPublisher,
            IPublisher<UserStatus> lobbyUserStatusPublisher,
            Identity identity,
            LocalGameState localGameState,
            LobbyUser localUser,
            LobbyContentHeartbeat lobbyContentHeartbeat,
            LobbyServiceData lobbyServiceData,
            LocalLobby localLobby,
            IInstanceResolver container,
            LocalLobbyFactory localLobbyFactory,
            NameGenerationData nameGenerationData,
            GameNetPortal gameNetPortal,
            ClientGameNetPortal clientGameNetPortal
        )
        {
            Application.wantsToQuit += OnWantToQuit;

            //m_persistentPlayer = persistentPlayer;

            m_NameGenerationData = nameGenerationData;
            m_localUser = localUser;
            _container = container;
            m_LobbyAsyncRequests = lobbyAsyncRequests;
            m_DisplayErrorPopupPublisher = displayErrorPopupPublisher;
            m_LobbyUserStatusPublisher = lobbyUserStatusPublisher;
            m_Identity = identity;
            m_localGameState = localGameState;
            m_lobbyContentHeartbeat = lobbyContentHeartbeat;
            m_lobbyServiceData = lobbyServiceData;
            m_LocalLobbyFactory = localLobbyFactory;
            m_localLobby = localLobby;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;
            m_localLobby.State = LobbyState.Lobby;

            RegenerateName();

            SubscribeToMessageChannels();


            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            m_ClientNetPortal.OnUnityRelayJoinFailed += OnRelayJoinFailed;
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
                m_ClientNetPortal.OnUnityRelayJoinFailed -= OnRelayJoinFailed;
            }

            ForceLeaveAttempt();
        }

        /// <summary>
        ///     In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        ///     So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveAttempt();
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            var canQuit = string.IsNullOrEmpty(m_localLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        private void ForceLeaveAttempt()
        {
            UnusubscribeFromMessageChannels();
            if (!string.IsNullOrEmpty(m_localLobby?.LobbyID))
            {
                m_LobbyAsyncRequests.LeaveLobbyAsync(m_localLobby?.LobbyID, null, null);
                m_localLobby = null;
            }
        }

        private void SubscribeToMessageChannels()
        {
            var subscriptions = new DisposableGroup();

            subscriptions.Add(_container.Resolve<ISubscriber<ClientUserApproved>>().Subscribe(OnClientUserApproved));
            subscriptions.Add(_container.Resolve<ISubscriber<UserStatus>>().Subscribe(OnLobbyUserStatus));
            subscriptions.Add(_container.Resolve<ISubscriber<StartCountdown>>().Subscribe(OnStartCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<CancelCountdown>>().Subscribe(OnCancelCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<CompleteCountdown>>().Subscribe(OnCompleteCountdown));
            //subscriptions.Add(_container.Resolve<ISubscriber<ChangeGameState>>().Subscribe(OnChangeGameState));
            subscriptions.Add(_container.Resolve<ISubscriber<ConfirmInGameState>>().Subscribe(OnConfirmInGameState));

            m_DisposableSubscriptions = subscriptions;

            void OnClientUserApproved(ClientUserApproved _)
            {
                ConfirmApproval();
            }

            void OnLobbyUserStatus(UserStatus status)
            {
                m_localUser.UserStatus = status;
            }

            void OnStartCountdown(StartCountdown _)
            {
                m_localLobby.State = LobbyState.CountDown;
            }

            void OnCancelCountdown(CancelCountdown _)
            {
                m_localLobby.State = LobbyState.Lobby;
            }

            void OnCompleteCountdown(CompleteCountdown _)
            {
                //todo:
                // if (m_relayClient is RelayUtpHost)
                //     (m_relayClient as RelayUtpHost).SendInGameState();
            }

            // void OnChangeGameState(ChangeGameState msg)
            // {   SetGameState(msg.GameState);
            // }

            void OnConfirmInGameState(ConfirmInGameState _)
            {
                m_localUser.UserStatus = UserStatus.InGame;
                m_localLobby.State = LobbyState.InGame;
            }
        }

        #endregion

        #region Lobby API

        public void CreateLobbyRequest(string lobbyName, bool isPrivate, int maxPlayers, OnlineMode onlineMode, string ip, int port)
        {
            m_LobbyAsyncRequests.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, onlineMode, ip, port, OnCreatedLobby, OnFailedJoin);

            BlockUIWhileLoadingIsInProgress();
        }

        public void QueryLobbiesRequest(bool blockUI)
        {
            m_lobbyServiceData.State = LobbyQueryState.Fetching;

            m_LobbyAsyncRequests.RetrieveLobbyListAsync(
                OnSuccess,
                OnFailure
            );

            if (blockUI) BlockUIWhileLoadingIsInProgress();

            void OnSuccess(QueryResponse qr)
            {
                UnblockUIAfterLoadingIsComplete();
                var localLobbies = m_LocalLobbyFactory.CreateLocalLobbies(qr);

                var newLobbyDict = new Dictionary<string, LocalLobby>();

                foreach (var lobby in localLobbies) newLobbyDict.Add(lobby.LobbyID, lobby);

                m_lobbyServiceData.FetchedLobbies(newLobbyDict);
            }

            void OnFailure()
            {
                UnblockUIAfterLoadingIsComplete();
                m_lobbyServiceData.State = LobbyQueryState.Error;
            }
        }


        public void JoinLobbyRequest(LocalLobby.LobbyData lobbyData)
        {
            m_LobbyAsyncRequests.JoinLobbyAsync(lobbyData.LobbyID, lobbyData.LobbyCode, m_localUser, OnJoinedLobby, OnFailedJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        private void UnusubscribeFromMessageChannels()
        {
            m_DisposableSubscriptions?.Dispose();
        }

        public void QuickJoinRequest()
        {
            m_LobbyAsyncRequests.QuickJoinLobbyAsync(m_localUser, OnJoinedLobby, OnFailedJoin);
            BlockUIWhileLoadingIsInProgress();
        }

        public void EndGame()
        {
            m_localLobby.State = LobbyState.Lobby;
            SetUserLobbyState();
        }

        private void SetGameState(GameState state)
        {
            var isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) && m_localGameState.State == GameState.Lobby;
            m_localGameState.State = state;
            if (isLeavingLobby)
                OnLeftLobby();
        }

        private void OnCreatedLobby(Lobby r)
        {
            m_localLobby.ApplyRemoteData(r);
            m_localUser.IsHost = true;
            Debug.Log($"Created lobby code: {m_localLobby.LobbyCode}");

            m_LobbyAsyncRequests.BeginTracking();
            m_lobbyContentHeartbeat.BeginTracking();

            SetUserLobbyState();

            // The host has the opportunity to reject incoming players, but to do so the player needs to connect to Relay without having game logic available.
            // In particular, we should prevent players from joining voice chat until they are approved.
            m_LobbyUserStatusPublisher.Publish(UserStatus.Connecting);

            Debug.Log(
                "We have created a lobby, so now we are starting the actual connection OR fetching relay codes to go into relay-based connection. This is not considered the final part of lobby being created: we would want the host to either start it's IP-based NGO game or the host needs to do that via relay");


            m_GameNetPortal.PlayerName = m_localUser.DisplayName;

            var cancellationTokenSource = new CancellationTokenSource();


            switch (m_localLobby.OnlineMode)
            {
                case OnlineMode.PhotonRelay:
                    throw new NotImplementedException();
                //m_GameNetPortal.StartPhotonRelayHost(connectInput, cancellationTokenSource.Token);
                case OnlineMode.IpHost:
                    m_GameNetPortal.StartHost(m_localLobby.Data.IP, m_localLobby.Data.Port);
                    break;

                case OnlineMode.UnityRelay:
                    Debug.Log("Unity Relay Host clicked");
                    m_GameNetPortal.StartUnityRelayHost(cancellationTokenSource.Token);
                    break;
            }
            //todo: add a "cancel" button and a label to show the current status. Cancel button would trigger cancellationTokenSource to cancel
            // m_ResponsePopup.SetupNotifierDisplay("Starting host", "Attempting to Start host...", true, false, () =>
            // {
            //     // Shutdown NetworkManager in case it started the hosting process
            //     m_GameNetPortal.RequestDisconnect();
            //     // This token is used with Photon Relay and Unity Relay to prevent starting the host if it hasn't yet
            //     cancellationTokenSource.Cancel();
            // });
        }

        private void OnJoinedLobby(Lobby remoteLobby)
        {
            m_localLobby.ApplyRemoteData(remoteLobby);

            m_LobbyAsyncRequests.BeginTracking();
            m_lobbyContentHeartbeat.BeginTracking();

            SetUserLobbyState();

            // The host has the opportunity to reject incoming players, but to do so the player needs to connect to Relay without having game logic available.
            // In particular, we should prevent players from joining voice chat until they are approved.
            m_LobbyUserStatusPublisher.Publish(UserStatus.Connecting);

            Debug.Log(
                "We're in lobby, so now we are starting the actual connection OR fetching relay codes to go into relay-based connection. This is not considered the final part of lobby being created: we would want the host to either start it's IP-based NGO game or the host needs to do that via relay");


            m_GameNetPortal.PlayerName = m_localUser.DisplayName;

            var cancellationTokenSource = new CancellationTokenSource();

            switch (m_localLobby.OnlineMode)
            {
                case OnlineMode.PhotonRelay:
                    throw new NotImplementedException();
                // if (ClientGameNetPortal.StartClientRelayMode(m_GameNetPortal, connectInput, out string failMessage, cancellationTokenSource.Token) == false)
                // {
                //     m_ResponsePopup.SetupNotifierDisplay("Connection Failed", failMessage, false, true);
                //     return;
                // }

                case OnlineMode.IpHost:
                    m_ClientNetPortal.StartClient(m_GameNetPortal, m_localLobby.Data.IP, m_localLobby.Data.Port);
                    break;

                case OnlineMode.UnityRelay:
                    Debug.Log($"Unity Relay Client, join code {m_localLobby.RelayCode}");
                    m_ClientNetPortal.StartClientUnityRelayModeAsync(m_GameNetPortal, m_localLobby.RelayCode, cancellationTokenSource.Token);
                    break;
            }

            //todo: add a "cancel" button and a label to show the current status. Cancel button would trigger cancellationTokenSource to cancel
            // m_ResponsePopup.SetupNotifierDisplay("Connecting", "Attempting to Join...", true, false, () =>
            // {
            //     // Shutdown NetworkManager in case it started the connection process
            //     m_GameNetPortal.RequestDisconnect();
            //     // This token is used with Photon Relay and Unity Relay to prevent starting the connection if it hasn't yet
            //     cancellationTokenSource.Cancel();
            // });
        }

        #endregion

        #region show/hide UI

        public void Show()
        {
            m_CanvasGroup.alpha = 1;
            UnblockUIAfterLoadingIsComplete();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0;
            BlockUIWhileLoadingIsInProgress();
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
            m_CanvasGroup.blocksRaycasts = false;
            m_LoadingSpinner.SetActive(true);
        }

        private void UnblockUIAfterLoadingIsComplete()
        {
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
            m_LoadingSpinner.SetActive(false);
        }

        #endregion


        #region m_clientNetPortal callbacks

        /// <summary>
        ///     Callback when the server sends us back a connection finished event.
        /// </summary>
        /// <param name="status"></param>
        private void OnConnectFinished(ConnectStatus status)
        {
            ConnectStatusToMessage(status, true);
        }

        private void OnRelayJoinFailed(string message)
        {
            UnblockUIAfterLoadingIsComplete();
            SetupNotifierDisplay("Unity Relay: Join Failed", $"{message}", false, true);
        }

        /// <summary>
        ///     Invoked when the client sent a connection request to the server and didn't hear back at all.
        ///     This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        private void OnNetworkTimeout()
        {
            UnblockUIAfterLoadingIsComplete();
            SetupNotifierDisplay("Connection Failed", "Unable to Reach Host/Server", false, true, "Please try again");
        }

        #endregion


        #region Notifier panel stuff

        /// <summary>
        ///     Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="displayImage">set to true if the notifier should display the animating icon for being busy</param>
        /// <param name="displayConfirmation"> set to true if the panel expects the user to click the button to close the panel.</param>
        /// <param name="subText">optional text in the middle of the panel.  Is not meant to coincide with the displayImage</param>
        public void SetupNotifierDisplay(string titleText, string mainText, bool displayImage, bool displayConfirmation, string subText = "")
        {
            Debug.Log($"{titleText}, {mainText}");

            // ResetState();
            //
            // m_TitleText.text = titleText;
            // m_MainText.text = mainText;
            // m_SubText.text = subText;
            //
            // m_ReconnectingImage.SetActive(displayImage);
            //
            // m_ConfirmationButton.gameObject.SetActive(displayConfirmation);
            // m_InputField.gameObject.SetActive(false);
            // m_PortInputField.gameObject.SetActive(false);
            // gameObject.SetActive(true);
        }

        /// <summary>
        ///     Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="displayImage">set to true if the notifier should display the animating icon for being busy</param>
        /// <param name="displayConfirmation"> set to true if the panel expects the user to click the button to close the panel.</param>
        /// <param name="cancelCallback"> The delegate to invoke when the player cancels. </param>
        /// <param name="subText">optional text in the middle of the panel.  Is not meant to coincide with the displayImage</param>
        public void SetupNotifierDisplay(string titleText, string mainText, bool displayImage, bool displayConfirmation, Action cancelCallback, string subText = "")
        {
            SetupNotifierDisplay(titleText, mainText, displayImage, displayConfirmation, subText);

            // m_CancelFunction = cancelCallback;
            // m_CancelButton.gameObject.SetActive(true);
            // m_CancelButton.onClick.AddListener(OnCancelClick);
        }

        #endregion

        #region OLD CODE

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
                    SetupNotifierDisplay("Connection Failed", "The Host is full and cannot accept any additional connections", false, true);
                    break;
                case ConnectStatus.Success:
                    if (connecting) SetupNotifierDisplay("Success!", "Joining Now", false, true);

                    break;
                case ConnectStatus.LoggedInAgain:
                    SetupNotifierDisplay("Connection Failed", "You have logged in elsewhere using the same account", false, true);
                    break;
                case ConnectStatus.GenericDisconnect:
                    var title = connecting ? "Connection Failed" : "Disconnected From Host";
                    var text = connecting ? "Something went wrong" : "The connection to the host was lost";
                    SetupNotifierDisplay(title, text, false, true);
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }


        private void StartRelayConnection()
        {
            // if (m_localUser.IsHost)
            //     m_relaySetup = gameObject.AddComponent<RelayUtpSetupHost>();
            // else
            //     m_relaySetup = gameObject.AddComponent<RelayUtpSetupClient>();
            // m_relaySetup.BeginRelayJoin(m_localLobby, m_localUser, OnRelayConnected);
            //
            // void OnRelayConnected(bool didSucceed, RelayUtpClient client)
            // {
            //     Component.Destroy(m_relaySetup);
            //     m_relaySetup = null;
            //
            //     if (!didSucceed)
            //     {   Debug.LogError("Relay connection failed! Retrying in 5s...");
            //         StartCoroutine(RetryConnection(StartRelayConnection, m_localLobby.LobbyID));
            //         return;
            //     }
            //
            //     m_relayClient = client;
            //     if (m_localUser.IsHost)
            //         CompleteRelayConnection();
            //     else
            //         Debug.Log("Client is now waiting for approval...");
            // }
        }


        private void OnLeftLobby()
        {
            m_lobbyContentHeartbeat.EndTracking();
            m_LobbyAsyncRequests.EndTracking();
            m_localUser.ResetState();
            m_LobbyAsyncRequests.LeaveLobbyAsync(m_localLobby.LobbyID, ResetLocalLobby, null);


            //todo: CLEANUP WHATEVER CONNECTION SETUP FOR THE LOBBY TYPE WE WERE IN
            //CleanupRelayConnection();
        }

        private void CleanupRelayConnection()
        {
            // if (m_relaySetup != null)
            // {   Component.Destroy(m_relaySetup);
            //     m_relaySetup = null;
            // }
            // if (m_relayClient != null)
            // {
            //     m_relayClient.Dispose();
            //     StartCoroutine(FinishCleanup());
            //
            //     // We need to delay slightly to give the disconnect message sent during Dispose time to reach the host, so that we don't destroy the connection without it being flushed first.
            //     IEnumerator FinishCleanup()
            //     {
            //         yield return null;
            //         Component.Destroy(m_relayClient);
            //         m_relayClient = null;
            //     }
            // }
        }

        /// <summary>
        ///     Back to Join menu if we fail to join for whatever reason.
        /// </summary>
        private void OnFailedJoin()
        {
            UnblockUIAfterLoadingIsComplete();
            SetGameState(GameState.JoinMenu);
        }


        private IEnumerator RetryConnection(Action doConnection, string lobbyId)
        {
            yield return new WaitForSeconds(5);
            if (m_localLobby != null && m_localLobby.LobbyID == lobbyId && !string.IsNullOrEmpty(lobbyId)) // Ensure we didn't leave the lobby during this waiting period.
                doConnection?.Invoke();
        }

        private void ConfirmApproval()
        {
            if (!m_localUser.IsHost && m_localUser.IsApproved) CompleteRelayConnection();
        }

        private void CompleteRelayConnection()
        {
            m_LobbyUserStatusPublisher.Publish(UserStatus.Lobby);
        }

        private void SetUserLobbyState()
        {
            SetGameState(GameState.Lobby);
            m_LobbyUserStatusPublisher.Publish(UserStatus.Lobby);
        }

        private void ResetLocalLobby()
        {
            m_localLobby.CopyObserved(new LocalLobby.LobbyData(), new Dictionary<string, LobbyUser>());
            m_localLobby.AddPlayer(m_localUser); // As before, the local player will need to be plugged into UI before the lobby join actually happens.
            m_localLobby.RelayServer = null;
        }

        #endregion
    }
}
