using System;
using System.Collections;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using BossRoom.Scripts.Shared.Net.UnityServices.Relays;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using GameState = BossRoom.Scripts.Shared.Net.UnityServices.Lobbies.GameState;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Game
{
    public class LobbyUIManager : MonoBehaviour
    {
        private DIScope _container;

        private IDisposable m_DisposableSubscriptions;

        //injected dependencies
        private LobbyAsyncRequests m_LobbyAsyncRequests;
        private LobbyUser m_localUser;
        private LocalLobby m_localLobby;
        private LobbyServiceData m_lobbyServiceData;
        private LobbyContentHeartbeat m_lobbyContentHeartbeat;
        private IPublisher<DisplayErrorPopup> m_DisplayErrorPopupPublisher;
        private IPublisher<UserStatus> m_LobbyUserStatusPublisher;
        private Identity m_Identity;
        private LocalGameState m_localGameState;
        //this one is currently manually collected from a player NetworkObject
        private PersistentPlayer m_persistentPlayer;

        private void Awake()
        {
            _container = new DIScope();

            _container.BindMessageChannel<ClientUserSeekingDisapproval>();
            _container.BindMessageChannel<DisplayErrorPopup>();
            _container.BindMessageChannel<CreateLobbyRequest>();
            _container.BindMessageChannel<JoinLobbyRequest>();
            _container.BindMessageChannel<QueryLobbies>();
            _container.BindMessageChannel<QuickJoin>();
            _container.BindMessageChannel<RenameRequest>();
            _container.BindMessageChannel<ClientUserApproved>();
            _container.BindMessageChannel<UserStatus>();
            _container.BindMessageChannel<StartCountdown>();
            _container.BindMessageChannel<CancelCountdown>();
            _container.BindMessageChannel<CompleteCountdown>();
            _container.BindMessageChannel<ChangeGameState>();
            _container.BindMessageChannel<ConfirmInGameState>();
            _container.BindMessageChannel<EndGame>();

            _container.BindAsSingle<LobbyAsyncRequests>();
            _container.BindAsSingle<LocalGameState>();
            _container.BindAsSingle<LobbyUser>();
            _container.BindAsSingle<LobbyServiceData>();
            _container.BindAsSingle<LobbyContentHeartbeat>();
            _container.BindAsSingle<LocalLobby>();

            _container.BindInstanceAsSingle(new Identity(OnAuthSignIn));

            _container.FinalizeScopeConstruction();

            //todo:
            // - break apart the initialization logic for the DI container into an entrypoint monobeh
            // - then the Lobby UI Manager would have a bunch of injected fields
            // - need to create a way to inject MonoBehaviours that I can set up from Inspector

            //todo: inject all monobehaviours of this UI so that they have the dependencies and can operate

            Application.wantsToQuit += OnWantToQuit;

            var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkManager.Singleton.LocalClientId);
            m_persistentPlayer = playerNetworkObject.GetComponent<PersistentPlayer>();

            SetLocalUserNameFromPersistentPlayerName();

            //fetching dependency references from the DI Scope
            m_LobbyAsyncRequests = _container.Resolve<LobbyAsyncRequests>();
            m_DisplayErrorPopupPublisher = _container.Resolve<IPublisher<DisplayErrorPopup>>();
            m_LobbyUserStatusPublisher = _container.Resolve<IPublisher<UserStatus>>();
            m_Identity = _container.Resolve<Identity>();
            m_localGameState = _container.Resolve<LocalGameState>();
            m_localUser = _container.Resolve<LobbyUser>();
            m_lobbyContentHeartbeat = _container.Resolve<LobbyContentHeartbeat>();
            m_lobbyServiceData = _container.Resolve<LobbyServiceData>();
            m_localLobby = _container.Resolve<LocalLobby>();
            m_localLobby.State = LobbyState.Lobby;

            SubscribeToMessageChannels();
        }

        private void OnDestroy()
        {
            _container?.Dispose();
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

            subscriptions.Add(_container.Resolve<ISubscriber<CreateLobbyRequest>>().Subscribe(OnCreateLobbyRequest));
            subscriptions.Add(_container.Resolve<ISubscriber<JoinLobbyRequest>>().Subscribe(OnJoinLobbyRequest));
            subscriptions.Add(_container.Resolve<ISubscriber<QueryLobbies>>().Subscribe(OnQueryLobbies));
            subscriptions.Add(_container.Resolve<ISubscriber<QuickJoin>>().Subscribe(OnQuickJoin));
            subscriptions.Add(_container.Resolve<ISubscriber<RenameRequest>>().Subscribe(OnRenameRequest));
            subscriptions.Add(_container.Resolve<ISubscriber<ClientUserApproved>>().Subscribe(OnClientUserApproved));
            subscriptions.Add(_container.Resolve<ISubscriber<UserStatus>>().Subscribe(OnLobbyUserStatus));
            subscriptions.Add(_container.Resolve<ISubscriber<StartCountdown>>().Subscribe(OnStartCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<CancelCountdown>>().Subscribe(OnCancelCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<CompleteCountdown>>().Subscribe(OnCompleteCountdown));
            subscriptions.Add(_container.Resolve<ISubscriber<ChangeGameState>>().Subscribe(OnChangeGameState));
            subscriptions.Add(_container.Resolve<ISubscriber<ConfirmInGameState>>().Subscribe(OnConfirmInGameState));
            subscriptions.Add(_container.Resolve<ISubscriber<EndGame>>().Subscribe(OnEndGame));

            m_DisposableSubscriptions = subscriptions;


            void OnCreateLobbyRequest(CreateLobbyRequest msg)
            {
                var createLobbyData = msg.CreateLobbyData;
                m_LobbyAsyncRequests.CreateLobbyAsync(createLobbyData.LobbyName, createLobbyData.MaxPlayerCount, createLobbyData.Private, m_localUser, (r) =>
                    {   ToLocalLobby.Convert(r, m_localLobby);
                        OnCreatedLobby();
                    },
                    OnFailedJoin);
            }

            void OnJoinLobbyRequest(JoinLobbyRequest msg)
            {
                var joinLobbyData = msg.JoinLobbyData;
                m_LobbyAsyncRequests.JoinLobbyAsync(joinLobbyData.LobbyID, joinLobbyData.LobbyCode, m_localUser, (r) =>
                    {   ToLocalLobby.Convert(r, m_localLobby);
                        OnJoinedLobby();
                    },
                    OnFailedJoin);
            }

            void OnQueryLobbies(QueryLobbies _)
            {
                m_lobbyServiceData.State = LobbyQueryState.Fetching;
                m_LobbyAsyncRequests.RetrieveLobbyListAsync(
                    qr => {
                        if (qr != null)
                            OnLobbiesQueried(ToLocalLobby.Convert(qr));
                    },
                    er => {
                        OnLobbyQueryFailed();
                    },
                    LobbyColor.None);
            }

            void OnQuickJoin(QuickJoin _)
            {
                m_LobbyAsyncRequests.QuickJoinLobbyAsync(m_localUser, LobbyColor.None, (r) =>
                    {   ToLocalLobby.Convert(r, m_localLobby);
                        OnJoinedLobby();
                    },
                    OnFailedJoin);
            }

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

            void OnEndGame(EndGame _)
            {   m_localLobby.State = LobbyState.Lobby;
                SetUserLobbyState();
            }
        }

        private void UnusubscribeFromMessageChannels()
        {
            m_DisposableSubscriptions?.Dispose();
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = m_Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            SetLocalUserNameFromPersistentPlayerName();
            m_localLobby.AddPlayer(m_localUser); // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
        }

        private void SetLocalUserNameFromPersistentPlayerName()
        {
            m_localUser.DisplayName = m_persistentPlayer.NetworkNameState.Name.Value;
        }


        //todo:
        // - *Create a centralized way to subscribe to Update, SlowUpdate, WantsToQuit and other events - use MessageChannel for that? Or reuse SlowUpdate
        // - Before we go into any kind of state that would require us to have wired dependencies - we should already had created a DIScope with dependencies
        //    - for the purposes of wiring the Lobby code we can just create a DI Scope within the PopupPanel (or some other class that would serve as entrypoint into this whole login logic)
        // - we want to start by injecting dependencies into our UI objects
        // - we want to suscribe to various events


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


    }
}
