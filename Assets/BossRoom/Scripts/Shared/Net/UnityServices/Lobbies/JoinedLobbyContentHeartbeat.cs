using System;
using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Keep updated on changes to a joined lobby, at a speed compliant with Lobby's rate limiting.
    /// </summary>
    public class JoinedLobbyContentHeartbeat
    {
        private readonly LocalLobby m_LocalLobby;
        private readonly LocalLobbyUser m_LocalUser;
        private readonly UpdateRunner m_UpdateRunner;
        private readonly LobbyServiceFacade m_LobbyServiceFacade;

        private int m_AwaitingQueryCount = 0;
        private bool m_ShouldPushData = false;


        [Inject]
        public JoinedLobbyContentHeartbeat(
            UpdateRunner updateRunner,
            LobbyServiceFacade lobbyServiceFacade,
            LocalLobby localLobby,
            LocalLobbyUser localUser)
        {
            m_UpdateRunner = updateRunner;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_LocalUser = localUser;
        }

        public void BeginTracking()
        {
            m_UpdateRunner.Subscribe(OnUpdate, 1.5f);
            m_LocalLobby.Changed += OnLocalLobbyChanged;
            m_ShouldPushData = true; // Ensure the initial presence of a new player is pushed to the lobby; otherwise, when a non-host joins, the LocalLobby never receives their data until they push something new.
        }

        public void EndTracking()
        {
            m_ShouldPushData = false;
            m_UpdateRunner.Unsubscribe(OnUpdate);
            m_LocalLobby.Changed -= OnLocalLobbyChanged;
        }

        private void OnLocalLobbyChanged(LocalLobby lobby)
        {
            if (string.IsNullOrEmpty(lobby.LobbyID)) // When the player leaves, their LocalLobby is cleared out but maintained.
            {
                EndTracking();
            }

            m_ShouldPushData = true;
        }


        /// <summary>
        /// If there have been any data changes since the last update, push them to Lobby.
        /// (Unless we're already awaiting a query, in which case continue waiting.)
        /// </summary>
        private void OnUpdate(float dt)
        {
            if (m_AwaitingQueryCount > 0)
            {
                return;
            }

            if (m_LocalUser.IsHost)
            {
                m_LobbyServiceFacade.DoLobbyHeartbeat(dt);
            }

            if (m_ShouldPushData)
            {
                m_ShouldPushData = false;

                if (m_LocalUser.IsHost)
                {
                    m_AwaitingQueryCount++;
                    m_LobbyServiceFacade.UpdateLobbyDataAsync(m_LocalLobby.GetDataForUnityServices(), OnSuccess, null);
                }
                m_AwaitingQueryCount++;
                m_LobbyServiceFacade.UpdatePlayerDataAsync(m_LocalUser.GetDataForUnityServices(), OnSuccess, null);
            }

            void OnSuccess()
            {
                m_AwaitingQueryCount--;
            }
        }
    }
}
