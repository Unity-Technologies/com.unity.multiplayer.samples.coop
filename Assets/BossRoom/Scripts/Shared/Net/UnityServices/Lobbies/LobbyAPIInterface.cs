using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    using Lobbies = Unity.Services.Lobbies.Lobbies;

    /// <summary>
    /// Wrapper for all the interactions with the Lobby API.
    /// </summary>
    public class LobbyAPIInterface
    {
        private const int k_maxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        private readonly IPublisher<DisplayErrorPopup> m_DisplayErrorPopupPublisher;

        [Inject]
        public LobbyAPIInterface(IPublisher<DisplayErrorPopup> displayErrorPopupPublisher)
        {
            m_DisplayErrorPopupPublisher = displayErrorPopupPublisher;
        }

        private void DoRequest(Task task, Action onComplete)
        {
            AsyncUnityServiceRequest.DoRequest<LobbyServiceException>(task, onComplete, ParseServiceException);
        }

        private void DoRequest<T>(Task<T> task, Action<T> onComplete)
        {
            AsyncUnityServiceRequest.DoRequest<T,LobbyServiceException>(task, onComplete, ParseServiceException);
        }

        private void ParseServiceException(LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.RateLimited) // We have other ways of preventing players from hitting the rate limit, so the developer-facing 429 error is sufficient here.
                return;
            var reason = $"Lobby Error: {e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.

            m_DisplayErrorPopupPublisher.Publish(new DisplayErrorPopup(reason));
        }

        public void CreateLobbyAsync(string requesterUASId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: requesterUASId, data: localUserData)
            };
            var task = Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
            DoRequest(task, onComplete);
        }

        public void DeleteLobbyAsync(string lobbyId, Action onComplete)
        {
            var task = Lobbies.Instance.DeleteLobbyAsync(lobbyId);
            DoRequest(task, onComplete);
        }

        public void JoinLobbyAsync_ByCode(string requesterUASId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            DoRequest(task, onComplete);
        }

        public void JoinLobbyAsync_ById(string requesterUASId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUASId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            DoRequest(task, onComplete);
        }

        public void QuickJoinLobbyAsync(string requesterUASId, List<QueryFilter> filters, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(id: requesterUASId, data: localUserData)
            };

            var task = Lobbies.Instance.QuickJoinLobbyAsync(joinRequest);
            DoRequest(task, onComplete);
        }

        public void LeaveLobbyAsync(string requesterUASId, string lobbyId, Action onComplete)
        {
            var task = Lobbies.Instance.RemovePlayerAsync(lobbyId, requesterUASId);
            DoRequest(task, onComplete);
        }

        public void QueryAllLobbiesAsync(List<QueryFilter> filters, Action<QueryResponse> onComplete)
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_maxLobbiesToShow,
                Filters = filters
            };
            var task = Lobbies.Instance.QueryLobbiesAsync(queryOptions);
            DoRequest(task, onComplete);
        }

        public void GetLobbyAsync(string lobbyId, Action<Lobby> onComplete)
        {
            var task = Lobbies.Instance.GetLobbyAsync(lobbyId);
            DoRequest(task, onComplete);
        }

        public void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, bool shouldLock, Action<Lobby> onComplete)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data , IsLocked = shouldLock};
            var task = Lobbies.Instance.UpdateLobbyAsync(lobbyId, updateOptions);
            DoRequest(task, onComplete);
        }

        public void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Lobby> onComplete, string allocationId, string connectionInfo)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            var task = Lobbies.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions);
            DoRequest(task, onComplete);
        }

        public void HeartbeatPlayerAsync(string lobbyId)
        {
            var task = Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            DoRequest(task, null);
        }
    }
}
