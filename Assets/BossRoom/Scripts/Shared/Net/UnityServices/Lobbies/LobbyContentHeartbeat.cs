﻿using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Relays;
using LobbyRemote = Unity.Services.Lobbies.Models.Lobby;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
    /// </summary>
    public class LobbyContentHeartbeat : IDisposable
    {
        private LocalLobby m_localLobby;
        private LobbyUser m_localUser;
        private int m_awaitingQueryCount = 0;
        private bool m_shouldPushData = false;

        private const float k_approvalMaxTime = 10; // Used for determining if a user should timeout if they are unable to connect.
        private float m_lifetime = 0;

        private readonly UpdateRunner m_SlowUpdate;
        private IDisposable m_DisposableSubscription;
        private readonly ISubscriber<ClientUserSeekingDisapproval> m_UserSeekingApprovalSubscriber;
        private readonly LobbyAsyncRequests m_LobbyAsyncRequests;
        private readonly IPublisher<UnityServiceErrorMessage> m_DisplayErrorPopupPublisher;
        private readonly IPublisher<ChangeGameState> m_ChangeGameStatePublisher;
        private readonly IPublisher<EndGame> m_EndGamePublisher;

        [Inject]
        public LobbyContentHeartbeat(
            UpdateRunner slowUpdate,
            ISubscriber<ClientUserSeekingDisapproval> userSeekingApprovalSubscriber,
            LobbyAsyncRequests lobbyAsyncRequests,
            IPublisher<ChangeGameState> changeGameStatePublisher,
            IPublisher<UnityServiceErrorMessage> displayErrorPopupPublisher,
            IPublisher<EndGame> endGamePublisher,
            LocalLobby localLobby,
            LobbyUser localUser)
        {
            m_SlowUpdate = slowUpdate;
            m_UserSeekingApprovalSubscriber = userSeekingApprovalSubscriber;
            m_LobbyAsyncRequests = lobbyAsyncRequests;
            m_ChangeGameStatePublisher = changeGameStatePublisher;
            m_DisplayErrorPopupPublisher = displayErrorPopupPublisher;
            m_EndGamePublisher = endGamePublisher;
            m_localLobby = localLobby;
            m_localUser = localUser;
        }

        public void Dispose()
        {
            m_DisposableSubscription?.Dispose();
        }

        public void BeginTracking()
        {
            m_SlowUpdate.Subscribe(OnUpdate, 1.5f);

            m_DisposableSubscription = m_UserSeekingApprovalSubscriber.Subscribe(OnReceiveMessage);

            m_localLobby.onChanged += OnLocalLobbyChanged;
            m_shouldPushData = true; // Ensure the initial presence of a new player is pushed to the lobby; otherwise, when a non-host joins, the LocalLobby never receives their data until they push something new.
            m_lifetime = 0;
        }

        public void EndTracking()
        {
            m_shouldPushData = false;
            m_SlowUpdate.Unsubscribe(OnUpdate);
            //disposing of subscription automatically removes the subscription from MessageChannel
            m_DisposableSubscription?.Dispose();
            if (m_localLobby != null)
                m_localLobby.onChanged -= OnLocalLobbyChanged;
        }

        private void OnLocalLobbyChanged(LocalLobby changed)
        {
            if (string.IsNullOrEmpty(changed.LobbyID)) // When the player leaves, their LocalLobby is cleared out but maintained.
                EndTracking();
            m_shouldPushData = true;
        }

        public void OnReceiveMessage(ClientUserSeekingDisapproval message)
        {
            bool shouldDisapprove = m_localLobby.State != LobbyState.Lobby; // By not refreshing, it's possible to have a lobby in the lobby list UI after its countdown starts and then try joining.
            if (shouldDisapprove)
            {
                message.DisapprovalAction?.Invoke(Approval.GameAlreadyStarted);
            }
        }

        /// <summary>
        /// If there have been any data changes since the last update, push them to Lobby. Regardless, pull for the most recent data.
        /// (Unless we're already awaiting a query, in which case continue waiting.)
        /// </summary>
        private void OnUpdate(float dt)
        {
            m_lifetime += dt;
            if (m_awaitingQueryCount > 0 || m_localLobby == null)
                return;
            if (m_localUser.IsHost)
            {
                m_LobbyAsyncRequests.DoLobbyHeartbeat(dt);
            }

            if (!m_localUser.IsApproved && m_lifetime > k_approvalMaxTime)
            {
                m_DisplayErrorPopupPublisher.Publish(new UnityServiceErrorMessage("Connection attempt timed out!"));
                m_ChangeGameStatePublisher.Publish(new ChangeGameState(GameState.JoinMenu));
            }

            if (m_shouldPushData)
            {
                PushDataToLobby();
            }
            else
            {
                OnRetrieve();
            }


            void PushDataToLobby()
            {
                m_shouldPushData = false;

                if (m_localUser.IsHost)
                {
                    m_awaitingQueryCount++;
                    m_LobbyAsyncRequests.UpdateLobbyDataAsync(m_localLobby.GetDataForUnityServices(), OnUpdatePushSuccess, null);
                }
                m_awaitingQueryCount++;
                m_LobbyAsyncRequests.UpdatePlayerDataAsync(m_localUser.GetDataForUnityServices(), OnUpdatePushSuccess, null);
            }

            void OnUpdatePushSuccess()
            {
                if (--m_awaitingQueryCount <= 0)
                {
                    OnRetrieve();
                }
            }

            void OnRetrieve()
            {
                LobbyRemote lobbyRemote = m_LobbyAsyncRequests.CurrentLobby;
                if (lobbyRemote == null) return;
                bool prevShouldPush = m_shouldPushData;
                var prevState = m_localLobby.State;
                m_localLobby.ApplyRemoteData(lobbyRemote);
                m_shouldPushData = prevShouldPush;

                // If the host suddenly leaves, the Lobby service will automatically handle disconnects after about 30s, but we can try to do a disconnect sooner if we detect it.
                if (!m_localUser.IsHost)
                {
                    foreach (var lobbyUser in m_localLobby.LobbyUsers)
                    {
                        if (lobbyUser.Value.IsHost)
                            return;
                    }
                    m_DisplayErrorPopupPublisher.Publish(new UnityServiceErrorMessage("Host left the lobby, disconnecting."));
                    m_EndGamePublisher.Publish(new EndGame());
                    m_ChangeGameStatePublisher.Publish(new ChangeGameState(GameState.JoinMenu));
                }
            }
        }




    }
}
