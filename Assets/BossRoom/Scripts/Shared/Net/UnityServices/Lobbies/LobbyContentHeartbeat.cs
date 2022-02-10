using System;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
    /// </summary>
    public class LobbyContentHeartbeat
    {
        private readonly LocalLobby m_localLobby;
        private readonly LobbyUser m_localUser;
        private int m_awaitingQueryCount = 0;
        private bool m_shouldPushData = false;

        private readonly UpdateRunner m_SlowUpdate;
        private IDisposable m_DisposableSubscription;
        private readonly LobbyAsyncRequests m_LobbyAsyncRequests;
        private readonly IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;
        private readonly Bootstrap m_Bootstrap;

        [Inject]
        public LobbyContentHeartbeat(
            UpdateRunner slowUpdate,
            LobbyAsyncRequests lobbyAsyncRequests,
            IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher,
            LocalLobby localLobby,
            LobbyUser localUser,
            Bootstrap bootstrap)
        {
            m_SlowUpdate = slowUpdate;
            m_LobbyAsyncRequests = lobbyAsyncRequests;
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;
            m_localLobby = localLobby;
            m_localUser = localUser;
            m_Bootstrap = bootstrap;
        }

        public void BeginTracking()
        {
            m_SlowUpdate.Subscribe(OnUpdate, 1.5f);

            m_localLobby.onChanged += OnLocalLobbyChanged;
            m_shouldPushData = true; // Ensure the initial presence of a new player is pushed to the lobby; otherwise, when a non-host joins, the LocalLobby never receives their data until they push something new.
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


        /// <summary>
        /// If there have been any data changes since the last update, push them to Lobby. Regardless, pull for the most recent data.
        /// (Unless we're already awaiting a query, in which case continue waiting.)
        /// </summary>
        private void OnUpdate(float dt)
        {
            if (m_awaitingQueryCount > 0)
                return;

            if (m_localUser.IsHost)
            {
                m_LobbyAsyncRequests.DoLobbyHeartbeat(dt);
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
                var lobbyRemote = m_LobbyAsyncRequests.CurrentLobby;
                if (lobbyRemote == null) return;
                bool prevShouldPush = m_shouldPushData;
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
                    m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Host left the lobby","Disconnecting."));
                    m_Bootstrap.QuitGame();
                }
            }
        }
    }
}
