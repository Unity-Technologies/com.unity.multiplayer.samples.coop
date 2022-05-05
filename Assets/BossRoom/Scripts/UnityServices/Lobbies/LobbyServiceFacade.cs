using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want.
    /// </summary>
    public class LobbyServiceFacade : IDisposable
    {
        readonly LobbyAPIInterface m_LobbyApiInterface;
        readonly UpdateRunner m_UpdateRunner;
        readonly LocalLobby m_LocalLobby;
        readonly LocalLobbyUser m_LocalUser;
        readonly JoinedLobbyContentHeartbeat m_JoinedLobbyContentHeartbeat;
        readonly IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePub;
        readonly IPublisher<LobbyListFetchedMessage> m_LobbyListFetchedPub;

        const float k_HeartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.
        float m_HeartbeatTime = 0;

        DIScope m_ServiceScope;

        RateLimitCooldown m_RateLimitQuery;
        RateLimitCooldown m_RateLimitJoin;
        RateLimitCooldown m_RateLimitQuickJoin;
        RateLimitCooldown m_RateLimitHost;

        public Lobby CurrentUnityLobby { get; private set; }

        bool m_IsTracking = false;

        [Inject]
        public LobbyServiceFacade(
            UpdateRunner updateRunner,
            LocalLobby localLobby,
            LocalLobbyUser localUser,
            IPublisher<UnityServiceErrorMessage> serviceErrorMessagePub,
            IPublisher<LobbyListFetchedMessage> lobbyListFetchedPub)
        {
            m_UpdateRunner = updateRunner;
            m_LocalLobby = localLobby;
            m_LocalUser = localUser;
            m_UnityServiceErrorMessagePub = serviceErrorMessagePub;
            m_LobbyListFetchedPub = lobbyListFetchedPub;

            m_ServiceScope = new DIScope(DIScope.RootScope);
            m_ServiceScope.BindInstanceAsSingle(this); //so that LobbyServiceFacade can get injected into whatever internal dependencies
            m_ServiceScope.BindAsSingle<JoinedLobbyContentHeartbeat>();
            m_ServiceScope.BindAsSingle<LobbyAPIInterface>();
            m_ServiceScope.FinalizeScopeConstruction();

            m_LobbyApiInterface = m_ServiceScope.Resolve<LobbyAPIInterface>();
            m_JoinedLobbyContentHeartbeat = m_ServiceScope.Resolve<JoinedLobbyContentHeartbeat>();

            //See https://docs.unity.com/lobby/rate-limits.html
            m_RateLimitQuery = new RateLimitCooldown(1f);
            m_RateLimitJoin = new RateLimitCooldown(3f);
            m_RateLimitQuickJoin = new RateLimitCooldown(10f);
            m_RateLimitHost = new RateLimitCooldown(3f);
        }

        public void Dispose()
        {
            EndTracking();
            m_ServiceScope?.Dispose();
        }

        public void SetRemoteLobby(Lobby lobby)
        {
            CurrentUnityLobby = lobby;
            m_LocalLobby.ApplyRemoteData(lobby);
        }

        public void BeginTracking()
        {
            if(!m_IsTracking)
            {
                m_IsTracking = true;
                // 2s update cadence is arbitrary and is here to demonstrate the fact that this update can be rather infrequent
                // the actual rate limits are tracked via the RateLimitCooldown objects defined above
                m_UpdateRunner.Subscribe(UpdateLobby, 2f);
                m_JoinedLobbyContentHeartbeat.BeginTracking();
            }
        }

        public Task EndTracking()
        {
            var task = Task.CompletedTask;
            if (CurrentUnityLobby != null)
            {
                CurrentUnityLobby = null;

                if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID))
                {
                    task = LeaveLobbyAsync(m_LocalLobby?.LobbyID);
                }

                m_LocalUser.ResetState();
                m_LocalLobby?.Reset(m_LocalUser);
            }

            if (m_IsTracking)
            {
                m_UpdateRunner.Unsubscribe(UpdateLobby);
                m_IsTracking = false;
                m_HeartbeatTime = 0;
                m_JoinedLobbyContentHeartbeat.EndTracking();
            }

            return task;
        }

        async void UpdateLobby(float unused)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                var lobby = await m_LobbyApiInterface.GetLobby(m_LocalLobby.LobbyID);

                CurrentUnityLobby = lobby;
                m_LocalLobby.ApplyRemoteData(lobby);

                // as client, check if host is still in lobby
                if (!m_LocalUser.IsHost)
                {
                    foreach (var lobbyUser in m_LocalLobby.LobbyUsers)
                    {
                        if (lobbyUser.Value.IsHost)
                        {
                            return;
                        }
                    }
                    m_UnityServiceErrorMessagePub.Publish(new UnityServiceErrorMessage("Host left the lobby","Disconnecting.", UnityServiceErrorMessage.Service.Lobby));
                    await EndTracking();
                    // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
            }
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryCreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate)
        {
            if (!m_RateLimitHost.CanCall)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = await m_LobbyApiInterface.CreateLobby(AuthenticationService.Instance.PlayerId, lobbyName, maxPlayers, isPrivate, m_LocalUser.GetDataForUnityServices(), null);
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitHost.PutOnCooldown();
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryJoinLobbyAsync(string lobbyId, string lobbyCode)
        {
            if (!m_RateLimitJoin.CanCall ||
                (lobbyId == null && lobbyCode == null))
            {
                Debug.LogWarning("Join Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                if (!string.IsNullOrEmpty(lobbyCode))
                {
                   var lobby = await  m_LobbyApiInterface.JoinLobbyByCode(AuthenticationService.Instance.PlayerId, lobbyCode, m_LocalUser.GetDataForUnityServices());
                   return (true, lobby);
                }
                else
                {
                    var lobby = await m_LobbyApiInterface.JoinLobbyById(AuthenticationService.Instance.PlayerId, lobbyId, m_LocalUser.GetDataForUnityServices());
                    return (true, lobby);
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitJoin.PutOnCooldown();
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered onlineMode.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryQuickJoinLobbyAsync()
        {
            if (!m_RateLimitQuickJoin.CanCall)
            {
                Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = await m_LobbyApiInterface.QuickJoinLobby(AuthenticationService.Instance.PlayerId, m_LocalUser.GetDataForUnityServices());
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuickJoin.PutOnCooldown();
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public async Task RetrieveAndPublishLobbyListAsync()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return;
            }

            try
            {
                var response = await m_LobbyApiInterface.QueryAllLobbies();
                m_LobbyListFetchedPub.Publish(new LobbyListFetchedMessage(LocalLobby.CreateLocalLobbies(response)));
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
            }
        }

        /// <summary>
        /// Attempt to leave a lobby
        /// </summary>
        public async Task LeaveLobbyAsync(string lobbyId)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            await m_LobbyApiInterface.RemovePlayerFromLobby(uasId, lobbyId);
        }

        public async void RemovePlayerFromLobbyAsync(string uasId, string lobbyId)
        {
            if (m_LocalUser.IsHost)
            {
                await m_LobbyApiInterface.RemovePlayerFromLobby(uasId, lobbyId);
            }
            else
            {
                Debug.LogError("Only the host can remove other players from the lobby.");
            }
        }

        public async void DeleteLobbyAsync(string lobbyId)
        {
            if (m_LocalUser.IsHost)
            {
                await m_LobbyApiInterface.DeleteLobby(lobbyId);
            }
            else
            {
                Debug.LogError("Only the host can delete a lobby.");
            }
        }

        /// <summary>
        /// Attempt to push a set of key-value pairs associated with the local player which will overwrite any existing data for these keys.
        /// </summary>
        public async Task UpdatePlayerDataAsync(Dictionary<string, PlayerDataObject> data)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                var result = await m_LobbyApiInterface.UpdatePlayer(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, data, null, null);

                if (result != null) {
                    CurrentUnityLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
            }
        }

        /// <summary>
        /// Lobby can be provided info about Relay (or any other remote allocation) so it can add automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerRelayInfoAsync(string allocationId, string connectionInfo)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                await m_LobbyApiInterface.UpdatePlayer(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, new Dictionary<string, PlayerDataObject>(), allocationId, connectionInfo);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }

                //todo - retry logic? SDK is supposed to handle this eventually
            }
        }

        /// <summary>
        /// Attempt to update a set of key-value pairs associated with a given lobby.
        /// </summary>
        public async Task UpdateLobbyDataAsync(Dictionary<string, DataObject> data)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            var dataCurr = CurrentUnityLobby.Data ?? new Dictionary<string, DataObject>();

            foreach (var dataNew in data)
            {
                if (dataCurr.ContainsKey(dataNew.Key))
                {
                    dataCurr[dataNew.Key] = dataNew.Value;
                }
                else
                {
                    dataCurr.Add(dataNew.Key, dataNew.Value);
                }
            }

            //we would want to lock lobbies from appearing in queries if we're in relay mode and the relay isn't fully set up yet
            var shouldLock = string.IsNullOrEmpty(m_LocalLobby.RelayJoinCode);

            try
            {
                var result = await m_LobbyApiInterface.UpdateLobby(CurrentUnityLobby.Id, dataCurr, shouldLock);

                if (result != null)
                {
                    CurrentUnityLobby = result;
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
            }
        }

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        public void DoLobbyHeartbeat(float dt)
        {
            m_HeartbeatTime += dt;
            if (m_HeartbeatTime > k_HeartbeatPeriod)
            {
                m_HeartbeatTime -= k_HeartbeatPeriod;
                m_LobbyApiInterface.SendHeartbeatPing(CurrentUnityLobby.Id);
            }
        }
    }
}
