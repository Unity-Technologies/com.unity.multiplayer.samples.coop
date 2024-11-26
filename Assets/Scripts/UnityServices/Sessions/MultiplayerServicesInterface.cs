using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.BossRoom.UnityServices.Sessions
{
    /// <summary>
    /// Wrapper for all the interactions with the Sessions API.
    /// </summary>
    public class MultiplayerServicesInterface
    {
        const int k_MaxSessionsToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.
        const int k_MaxPlayers = 8;

        readonly List<FilterOption> m_FilterOptions;
        readonly List<SortOption> m_SortOptions;

        public MultiplayerServicesInterface()
        {
            // Filter for open sessions only
            m_FilterOptions = new List<FilterOption>
            {
                new(FilterField.AvailableSlots, "0", FilterOperation.Greater)
            };

            // Order by newest sessions first
            m_SortOptions = new List<SortOption>
            {
                new(SortOrder.Descending, SortField.CreationTime)
            };
        }

        public async Task<ISession> CreateSession(string sessionName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerProperty> playerProperties, Dictionary<string, SessionProperty> sessionProperties)
        {
            var sessionOptions = new SessionOptions
            {
                Name = sessionName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = false,
                PlayerProperties = playerProperties,
                SessionProperties = sessionProperties
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
        }

        public async Task<ISession> JoinSessionByCode(string sessionCode, Dictionary<string, PlayerProperty> localUserData)
        {
            var joinSessionOptions = new JoinSessionOptions
            {
                PlayerProperties = localUserData
            };
            return await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode, joinSessionOptions);
        }

        public async Task<ISession> JoinSessionById(string sessionId, Dictionary<string, PlayerProperty> localUserData)
        {
            var joinSessionOptions = new JoinSessionOptions
            {
                PlayerProperties = localUserData
            };
            return await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, joinSessionOptions);
        }

        public async Task<ISession> QuickJoinSession(Dictionary<string, PlayerProperty> localUserData)
        {
            var quickJoinOptions = new QuickJoinOptions
            {
                Filters = m_FilterOptions,
                CreateSession = true // create a Session if no matching Session was found
            };

            var sessionOptions = new SessionOptions
            {
                MaxPlayers = k_MaxPlayers,
                PlayerProperties = localUserData
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
        }

        public async Task<QuerySessionsResults> QuerySessions()
        {
            return await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        }

        public async Task<ISession> ReconnectToSession(string sessionId)
        {
            return await MultiplayerService.Instance.ReconnectToSessionAsync(sessionId);
        }

        public async Task<QuerySessionsResults> QueryAllSessions()
        {
            var querySessionOptions = new QuerySessionsOptions
            {
                Count = k_MaxSessionsToShow,
                FilterOptions = m_FilterOptions,
                SortOptions = m_SortOptions
            };
            return await MultiplayerService.Instance.QuerySessionsAsync(querySessionOptions);
        }
    }
}
