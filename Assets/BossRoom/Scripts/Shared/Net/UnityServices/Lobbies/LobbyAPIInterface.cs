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

        async Task<T> ExceptionHandling<T>(Task<T> task)
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.

            try
            {
                return await task;
            }
            catch (Exception e)
            {
                OnServiceException(e);
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n");
                throw;
            }
        }

        async Task ExceptionHandling(Task task)
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.

            try
            {
                await task;
            }
            catch (Exception e)
            {
                OnServiceException(e);
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n");
                throw;
            }
        }

        void OnServiceException(Exception e)
        {
            Debug.LogException(e);
            var reason = $"{e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.
            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
        }

        public async Task<Lobby> CreateLobby(string requesterUasId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> hostUserData, Dictionary<string, DataObject> lobbyData)
        {
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: requesterUasId, data: hostUserData),
                Data = lobbyData
            };

            return await ExceptionHandling(Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions));
        }

        public async Task DeleteLobby(string lobbyId)
        {
            await ExceptionHandling(Lobbies.Instance.DeleteLobbyAsync(lobbyId));
        }

        public async Task<Lobby> JoinLobbyByCode(string requesterUasId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUasId, data: localUserData) };
            return await ExceptionHandling(Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions));
        }

        public async Task<Lobby> JoinLobbyById(string requesterUasId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUasId, data: localUserData) };
            return await ExceptionHandling(Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions));
        }

        public async Task<Lobby> QuickJoinLobby(string requesterUasId, Dictionary<string, PlayerDataObject> localUserData)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = m_Filters,
                Player = new Player(id: requesterUasId, data: localUserData)
            };
            return await ExceptionHandling(Lobbies.Instance.QuickJoinLobbyAsync(joinRequest));
        }

        public async Task RemovePlayerFromLobby(string requesterUasId, string lobbyId)
        {
           await ExceptionHandling(Lobbies.Instance.RemovePlayerAsync(lobbyId, requesterUasId));
        }

        public async Task<QueryResponse> QueryAllLobbies()
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_MaxLobbiesToShow,
                Filters = m_Filters,
                Order = m_Order
            };

            return await ExceptionHandling(Lobbies.Instance.QueryLobbiesAsync(queryOptions));
        }

        public async Task<Lobby> GetLobby(string lobbyId)
        {
            return await ExceptionHandling(Lobbies.Instance.GetLobbyAsync(lobbyId));
        }

        public async Task<Lobby> UpdateLobby(string lobbyId, Dictionary<string, DataObject> data, bool shouldLock)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data , IsLocked = shouldLock};
            return await ExceptionHandling(Lobbies.Instance.UpdateLobbyAsync(lobbyId, updateOptions));
        }

        public async Task<Lobby> UpdatePlayer(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, string allocationId, string connectionInfo)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            return await ExceptionHandling(Lobbies.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions));
        }

        public async Task SendHeartbeatPing(string lobbyId)
        {
            await ExceptionHandling(Lobbies.Instance.SendHeartbeatPingAsync(lobbyId));
        }
    }
}
