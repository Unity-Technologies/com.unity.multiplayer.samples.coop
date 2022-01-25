using System;
using System.Collections;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using GameLobby.UI;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameState = BossRoom.Scripts.Shared.Net.UnityServices.Lobbies.GameState;

namespace BossRoom.Scripts.Client.UI
{
    public class LobbyUIMediator : MonoBehaviour
    {
        //injected dependencies
        private IInstanceResolver _container;
        private LobbyAsyncRequests m_LobbyAsyncRequests;
        private LobbyUser m_localUser;
        private LocalLobby m_localLobby;
        private LobbyServiceData m_lobbyServiceData;
        private LobbyContentHeartbeat m_lobbyContentHeartbeat;
        private IPublisher<DisplayErrorPopup> m_DisplayErrorPopupPublisher;
        private IPublisher<UserStatus> m_LobbyUserStatusPublisher;
        private Identity m_Identity;
        private LocalGameState m_localGameState;

        private IDisposable m_DisposableSubscriptions;

        [SerializeField] private CanvasGroup _cg;
        [SerializeField] private CanvasGroup m_JoinLobbyCanvasGroup;
        [SerializeField] private CanvasGroup m_CreateLobbyCanvasGroup;
        [SerializeField] private UITinter m_JoinToggle;
        [SerializeField] private UITinter m_CreateToggle;

        [Inject]
        private void InjectDependencies(
            LobbyAsyncRequests lobbyAsyncRequests,
            IPublisher<DisplayErrorPopup> displayErrorPopupPublisher,
            IPublisher<UserStatus> lobbyUserStatusPublisher,
            Identity identity,
            LocalGameState localGameState,
            LobbyUser lobbyUser,
            LobbyContentHeartbeat lobbyContentHeartbeat,
            LobbyServiceData lobbyServiceData,
            LocalLobby localLobby,
            IInstanceResolver container
        )
        {
            Application.wantsToQuit += OnWantToQuit;

            //m_persistentPlayer = persistentPlayer;

            m_localUser = lobbyUser;
            m_localUser.DisplayName = "test";//m_persistentPlayer.NetworkNameState.Name.Value;

            _container = container;
            m_LobbyAsyncRequests = lobbyAsyncRequests;
            m_DisplayErrorPopupPublisher = displayErrorPopupPublisher;
            m_LobbyUserStatusPublisher = lobbyUserStatusPublisher;
            m_Identity = identity;
            m_localGameState = localGameState;
            m_lobbyContentHeartbeat = lobbyContentHeartbeat;
            m_lobbyServiceData = lobbyServiceData;
            m_localLobby = localLobby;
            m_localLobby.State = LobbyState.Lobby;

            SubscribeToMessageChannels();
        }

        private void Awake()
        {
            ShowJoinLobbyUI();
        }

        private void OnDestroy()
        {
            ForceLeaveAttempt();
        }

        /// <summary>
        /// In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        /// So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveAttempt();
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            bool canQuit = string.IsNullOrEmpty(m_localLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        private void ForceLeaveAttempt()
        {
            UnusubscribeFromMessageChannels();
            if (!string.IsNullOrEmpty(m_localLobby?.LobbyID))
            {
                m_LobbyAsyncRequests.LeaveLobbyAsync(m_localLobby?.LobbyID, null);
                m_localLobby = null;
            }
        }

        private void SubscribeToMessageChannels()
        {
            var subscriptions = new DisposableGroup();

            subscriptions.Add(_container.Resolve<ISubscriber<RenameRequest>>().Subscribe(OnRenameRequest));
            subscriptions.Add(_container.Resolve<ISubscriber<ClientUserApproved>>().Subscribe(OnClientUserApproved));
            subscriptions.Add(_container.Resolve<ISubscriber<UserStatus>>().Subscribe(OnLobbyUserStatus));
            subscriptions.Add(_container.Resolve<ISubscriber<StartCountdown>>().Subscribe(OnStartCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<CancelCountdown>>().Subscribe(OnCancelCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<CompleteCountdown>>().Subscribe(OnCompleteCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<ChangeGameState>>().Subscribe(OnChangeGameState));
            subscriptions.Add(_container.Resolve<ISubscriber<ConfirmInGameState>>().Subscribe(OnConfirmInGameState));

            m_DisposableSubscriptions = subscriptions;


            void OnRenameRequest(RenameRequest msg)
            {
                if (string.IsNullOrWhiteSpace(msg.Name))
                {
                    m_DisplayErrorPopupPublisher.Publish(new DisplayErrorPopup("Empty Name not allowed."));
                    return;
                }

                m_localUser.DisplayName = msg.Name;
            }

            void OnClientUserApproved(ClientUserApproved _)
            {
                ConfirmApproval();
            }


            void OnLobbyUserStatus(UserStatus status)
            {
                m_localUser.UserStatus = status;
            }

            void OnStartCountdown(StartCountdown _)
            {   m_localLobby.State = LobbyState.CountDown;
            }

            void OnCancelCountdown(CancelCountdown _)
            {   m_localLobby.State = LobbyState.Lobby;
            }

            void OnCompleteCountdown(CompleteCountdown _)
            {
                //todo:
                // if (m_relayClient is RelayUtpHost)
                //     (m_relayClient as RelayUtpHost).SendInGameState();
            }

            void OnChangeGameState(ChangeGameState msg)
            {   SetGameState(msg.GameState);
            }

            void OnConfirmInGameState(ConfirmInGameState _)
            {   m_localUser.UserStatus = UserStatus.InGame;
                m_localLobby.State = LobbyState.InGame;
            }


        }

        public void CreateLobbyRequest(LocalLobby.LobbyData lobbyData)
        {
            m_LobbyAsyncRequests.CreateLobbyAsync(lobbyData.LobbyName, lobbyData.MaxPlayerCount, lobbyData.Private, m_localUser, (r) =>
                {   ToLocalLobby.Convert(r, m_localLobby);
                    OnCreatedLobby();
                },
                OnFailedJoin);
        }

        public void QueryLobbiesRequest()
        {
            m_lobbyServiceData.State = LobbyQueryState.Fetching;
            m_LobbyAsyncRequests.RetrieveLobbyListAsync(
                qr => {
                    if (qr != null)
                        OnLobbiesQueried(ToLocalLobby.Convert(qr));
                },
                er => {
                    OnLobbyQueryFailed();
                });
        }


        public void JoinLobbyRequest(LocalLobby.LobbyData lobbyData)
        {
            m_LobbyAsyncRequests.JoinLobbyAsync(lobbyData.LobbyID, lobbyData.LobbyCode, m_localUser, (r) =>
                {   ToLocalLobby.Convert(r, m_localLobby);
                    OnJoinedLobby();
                },
                OnFailedJoin);
        }

        private void UnusubscribeFromMessageChannels()
        {
            m_DisposableSubscriptions?.Dispose();
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = m_Identity.GetSubIdentity(IIdentityType.Auth).GetContent("id");
            m_localUser.DisplayName = "test";// m_persistentPlayer.NetworkNameState.Name.Value;
            m_localLobby.AddPlayer(m_localUser); // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
        }

        public void QuickJoinRequest()
        {
            m_LobbyAsyncRequests.QuickJoinLobbyAsync(m_localUser, OnSuccess, OnFailedJoin);

            void OnSuccess(Lobby r)
            {
                ToLocalLobby.Convert(r, m_localLobby);
                OnJoinedLobby();
            }
        }

       public void EndGame()
        {
            m_localLobby.State = LobbyState.Lobby;
            SetUserLobbyState();
        }

       private void SetGameState(GameState state)
        {
            bool isLeavingLobby = (state == GameState.Menu || state == GameState.JoinMenu) && m_localGameState.State == GameState.Lobby;
            m_localGameState.State = state;
            if (isLeavingLobby)
                OnLeftLobby();
        }

        private void OnLobbiesQueried(IEnumerable<LocalLobby> lobbies)
        {
            var newLobbyDict = new Dictionary<string, LocalLobby>();
            foreach (var lobby in lobbies)
                newLobbyDict.Add(lobby.LobbyID, lobby);

            m_lobbyServiceData.State = LobbyQueryState.Fetched;
            m_lobbyServiceData.CurrentLobbies = newLobbyDict;
        }

        private void OnLobbyQueryFailed()
        {
            m_lobbyServiceData.State = LobbyQueryState.Error;
        }

        private void OnCreatedLobby()
        {
            m_localUser.IsHost = true;
            OnJoinedLobby();
        }

        private void OnJoinedLobby()
        {
            m_LobbyAsyncRequests.BeginTracking(m_localLobby.LobbyID);
            m_lobbyContentHeartbeat.BeginTracking(m_localLobby, m_localUser);
            SetUserLobbyState();

            // The host has the opportunity to reject incoming players, but to do so the player needs to connect to Relay without having game logic available.
            // In particular, we should prevent players from joining voice chat until they are approved.
            m_LobbyUserStatusPublisher.Publish(UserStatus.Connecting);

            //todo: ADD ABILITY TO CONNECT VIA OTHER MEANS THAN JUST RELAY (DIRECT IP, Photon Relay??)
            StartRelayConnection();
        }

        private void OnLeftLobby()
        {
            m_localUser.ResetState();
            m_LobbyAsyncRequests.LeaveLobbyAsync(m_localLobby.LobbyID, ResetLocalLobby);
            m_lobbyContentHeartbeat.EndTracking();
            m_LobbyAsyncRequests.EndTracking();

            //todo: CLEANUP WHATEVER CONNECTION SETUP FOR THE LOBBY TYPE WE WERE IN
            CleanupRelayConnection();

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
        /// Back to Join menu if we fail to join for whatever reason.
        /// </summary>
        private void OnFailedJoin()
        {
            SetGameState(GameState.JoinMenu);
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

        private IEnumerator RetryConnection(Action doConnection, string lobbyId)
        {
            yield return new WaitForSeconds(5);
            if (m_localLobby != null && m_localLobby.LobbyID == lobbyId && !string.IsNullOrEmpty(lobbyId)) // Ensure we didn't leave the lobby during this waiting period.
                doConnection?.Invoke();
        }

        private void ConfirmApproval()
        {
            if (!m_localUser.IsHost && m_localUser.IsApproved)
            {
                CompleteRelayConnection();
            }
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

        public void Show()
        {
            ShowCanvasGroup(_cg);
        }

        public void Hide()
        {
            HideCanvasGroup(_cg);
        }

        public void ShowJoinLobbyUI()
        {
            HideCanvasGroup(m_CreateLobbyCanvasGroup);
            ShowCanvasGroup(m_JoinLobbyCanvasGroup);
            m_JoinToggle.SetToColor(true);
            m_CreateToggle.SetToColor(false);
        }

        public void ShowCreateLobbyUI()
        {
            HideCanvasGroup(m_JoinLobbyCanvasGroup);
            ShowCanvasGroup(m_CreateLobbyCanvasGroup);
            m_JoinToggle.SetToColor(false);
            m_CreateToggle.SetToColor(true);
        }

        private void HideCanvasGroup(CanvasGroup cg)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        private void ShowCanvasGroup(CanvasGroup cg)
        {
            cg.alpha = 1;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
}
