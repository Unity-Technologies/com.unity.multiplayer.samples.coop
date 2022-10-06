using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.BossRoom.Infrastructure;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.UnityServices.Lobbies
{
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
            try
            {
                return await task;
            }
            catch (Exception e)
            {
                var reason = $"{e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
                throw;
            }
        }

        async Task ExceptionHandling(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                var reason = $"{e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
                throw;
            }
        }

        public async Task<Lobby> CreateLobby(string requesterUasId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, PlayerDataObject> hostUserData, Dictionary<string, DataObject> lobbyData)
        {
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: requesterUasId, data: hostUserData),
                Data = lobbyData
            };

            return await ExceptionHandling(LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions));
        }

        public async Task DeleteLobby(string lobbyId)
        {
            await ExceptionHandling(LobbyService.Instance.DeleteLobbyAsync(lobbyId));
        }

        public async Task<Lobby> JoinLobbyByCode(string requesterUasId, string lobbyCode, Dictionary<string, PlayerDataObject> localUserData)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions { Player = new Player(id: requesterUasId, data: localUserData) };
            return await ExceptionHandling(LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions));
        }

        public async Task<Lobby> JoinLobbyById(string requesterUasId, string lobbyId, Dictionary<string, PlayerDataObject> localUserData)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions { Player = new Player(id: requesterUasId, data: localUserData) };
            return await ExceptionHandling(LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions));
        }

        public async Task<Lobby> QuickJoinLobby(string requesterUasId, Dictionary<string, PlayerDataObject> localUserData)
        {
            var joinRequest = new QuickJoinLobbyOptions
            {
                Filter = m_Filters,
                Player = new Player(id: requesterUasId, data: localUserData)
            };

            return await ExceptionHandling(LobbyService.Instance.QuickJoinLobbyAsync(joinRequest));
        }

        public async Task<Lobby> ReconnectToLobby(string lobbyId)
        {
            return await ExceptionHandling(LobbyService.Instance.ReconnectToLobbyAsync(lobbyId));
        }

        public async Task RemovePlayerFromLobby(string requesterUasId, string lobbyId)
        {
            try
            {
                await ExceptionHandling(LobbyService.Instance.RemovePlayerAsync(lobbyId, requesterUasId));
            }
            catch (LobbyServiceException e)
                when (e is { Reason: LobbyExceptionReason.PlayerNotFound })
            {
                // If Player is not found, they have already left the lobby or have been kicked out. No need to throw here
            }
        }

        public async Task<QueryResponse> QueryAllLobbies()
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = k_MaxLobbiesToShow,
                Filters = m_Filters,
                Order = m_Order
            };

            return await ExceptionHandling(LobbyService.Instance.QueryLobbiesAsync(queryOptions));
        }

        public async Task<Lobby> GetLobby(string lobbyId)
        {
            return await ExceptionHandling(LobbyService.Instance.GetLobbyAsync(lobbyId));
        }

        public async Task<Lobby> UpdateLobby(string lobbyId, Dictionary<string, DataObject> data, bool shouldLock)
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = data, IsLocked = shouldLock };
            return await ExceptionHandling(LobbyService.Instance.UpdateLobbyAsync(lobbyId, updateOptions));
        }

        public async Task<Lobby> UpdatePlayer(string lobbyId, string playerId, Dictionary<string, PlayerDataObject> data, string allocationId, string connectionInfo)
        {
            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = data,
                AllocationId = allocationId,
                ConnectionInfo = connectionInfo
            };
            return await ExceptionHandling(LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, updateOptions));
        }

        public async void SendHeartbeatPing(string lobbyId)
        {
            await ExceptionHandling(LobbyService.Instance.SendHeartbeatPingAsync(lobbyId));
        }
    }
}
