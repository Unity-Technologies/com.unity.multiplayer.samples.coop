using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies
{
    using Lobbies = Unity.Services.Lobbies.Lobbies;

    /// <summary>
    /// Wrapper for all the interactions with the Lobby API.
    /// </summary>
    public class LobbyAPIInterface
    {
        const int k_MaxLobbiesToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        readonly IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;
        readonly List<QueryFilter> m_Filters;
        readonly List<QueryOrder> m_Order;

        [Inject]
        public LobbyAPIInterface(IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher)
        {
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;

            // Filter for open lobbies only
            m_Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            m_Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };
        }

        void RunTask(Task task, Action onComplete, Action onFailed)
        {
            UnityServiceCallsTaskWrapper.RunTaskAsync<LobbyServiceException>(task, onComplete, onFailed, OnServiceException);
        }

        void RunTask<T>(Task<T> task, Action<T> onComplete, Action onFailed)
        {
            UnityServiceCallsTaskWrapper.RunTaskAsync<T,LobbyServiceException>(task, onComplete, onFailed, OnServiceException);
        }

        void OnServiceException(LobbyServiceException e)
        {
            Debug.LogException(e);

            if (e.Reason == LobbyExceptionReason.RateLimited) // We have other ways of preventing players from hitting the rate limit, so the developer-facing 429 error is sufficient here.
            {
                // todo trigger client side rate limit here
                return;
            }

            var reason = $"{e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.

            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
        }

        public void CreateLobbyAsync(string requesterUasId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> hostUserData, Dictionary<string, DataObject> lobbyData, Action<Lobby> onComplete, Action onFailed)
        {
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: requesterUasId, data: hostUserData),
                Data = lobbyData
            };
            var task = Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
            RunTask(task, onComplete, onFailed);
        }

        public void DeleteLobbyAsync(string lobbyId, Action onComplete, Action onFailed)
        {
            var task = Lobbies.Instance.DeleteLobbyAsync(lobbyId);
            RunTask(task, onComplete, onFailed);
        }

        public void JoinLobbyAsync_ByCode(string requesterUasId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete, Action onFailed)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUasId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            RunTask(task, onComplete, onFailed);
        }

        public void JoinLobbyAsync_ById(string requesterUasId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete, Action onFailed)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUasId, data: localUserData) };
            var task = Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            RunTask(task, onComplete, onFailed);
        }

        public void QuickJoinLobbyAsync(string requesterUasId, Dictionary<string, PlayerDataObject> localUserData, Action<Lobby> onComplete, Action onFailed)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = m_Filters,
                Player = new Player(id: requesterUasId, data: localUserData)
            };

            var task = Lobbies.Instance.QuickJoinLobbyAsync(joinRequest);
            RunTask(task, onComplete, onFailed);
        }

        public void LeaveLobbyAsync(string requesterUasId, string lobbyId, Action onComplete, Action onFailed)
        {
            var task = Lobbies.Instance.RemovePlayerAsync(lobbyId, requesterUasId);
            RunTask(task, onComplete, onFailed);
        }

        public async Task<QueryResponse> QueryAllLobbiesAsync()
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_MaxLobbiesToShow,
                Filters = m_Filters,
                Order = m_Order
            };

            try
            {
                return await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
            }
            catch (LobbyServiceException serviceE)
            {
                OnServiceException(serviceE);
                throw;
            }
        }

        public void GetLobbyAsync(string lobbyId, Action<Lobby> onComplete, Action onFailed)
        {
            var task = Lobbies.Instance.GetLobbyAsync(lobbyId);
            RunTask(task, onComplete, onFailed);
        }

        public void UpdateLobbyAsync(string lobbyId, Dictionary<string, DataObject> data, bool shouldLock, Action<Lobby> onComplete, Action onFailed)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data , IsLocked = shouldLock};
            var task = Lobbies.Instance.UpdateLobbyAsync(lobbyId, updateOptions);
            RunTask(task, onComplete, onFailed);
        }

        public void UpdatePlayerAsync(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, Action<Lobby> onComplete, Action onFailed, string allocationId, string connectionInfo)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            var task = Lobbies.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions);
            RunTask(task, onComplete, onFailed);
        }

        public void HeartbeatPlayerAsync(string lobbyId)
        {
            var task = Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            RunTask(task, null, null);
        }
    }
}
