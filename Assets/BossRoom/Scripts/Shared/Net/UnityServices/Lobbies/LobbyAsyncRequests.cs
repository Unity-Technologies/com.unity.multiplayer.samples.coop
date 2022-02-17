using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want. E.g. you can request to get a readable list of
    /// current lobbies and not need to make the query call directly.
    /// </summary>
    public class LobbyAsyncRequests : IDisposable
    {
        private readonly LobbyAPIInterface m_LobbyApiInterface;
        private readonly UpdateRunner m_SlowUpdate;
        private readonly LocalLobby m_LocalLobby;
        private readonly LobbyUser m_LocalUser;
        private readonly LobbyContentHeartbeat m_LobbyContentHeartbeat;

        private const float k_heartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.

        private float m_heartbeatTime = 0;

        private RateLimitCooldown m_RateLimitQuery; // Used for both the lobby list UI and the in-lobby updating. In the latter case, updates can be cached.
        private RateLimitCooldown m_RateLimitJoin;
        private RateLimitCooldown m_RateLimitQuickJoin;
        private RateLimitCooldown m_RateLimitHost;

        [Inject]
        public LobbyAsyncRequests(UpdateRunner slowUpdate, LobbyAPIInterface lobbyAPIInterface, LocalLobby localLobby, LobbyUser localUser, LobbyContentHeartbeat lobbyContentHeartbeat)
        {
            m_LobbyApiInterface = lobbyAPIInterface;
            m_SlowUpdate = slowUpdate;
            m_LocalLobby = localLobby;
            m_LocalUser = localUser;
            m_LobbyContentHeartbeat = lobbyContentHeartbeat;

            m_RateLimitQuery = new RateLimitCooldown(1.5f, slowUpdate);
            m_RateLimitJoin = new RateLimitCooldown(3f, slowUpdate);
            m_RateLimitQuickJoin = new RateLimitCooldown(10f,  slowUpdate);
            m_RateLimitHost = new RateLimitCooldown(3f, slowUpdate);
        }

        public void Dispose()
        {
           EndTracking();
        }

        private Lobby m_lastKnownLobby;
        public Lobby CurrentLobby => m_lastKnownLobby;

        private bool m_isTracking = false;

        public void BeginTracking(Lobby lobby)
        {
            if(!m_isTracking)
            {
                m_isTracking = true;
                m_lastKnownLobby = lobby;
                // 1.5s update cadence is arbitrary and is here to demonstrate the fact that this update can be rather infrequent
                // the actual rate limits are tracked via the RateLimitCooldown objects defined above
                m_SlowUpdate.Subscribe(UpdateLobby, 1.5f);
            }
        }

        public void EndTracking()
        {
            if (m_isTracking)
            {
                m_SlowUpdate.Unsubscribe(UpdateLobby);
                m_isTracking = false;
                m_lastKnownLobby = null;
                m_heartbeatTime = 0;
            }
        }

        private void UpdateLobby(float unused)
        {
            RetrieveLobbyAsync(m_LocalLobby.LobbyID, OnSuccess, null);

            void OnSuccess(Lobby lobby)
            {
                //todo: send a messagechannel message that lobby was updated
                m_lastKnownLobby = lobby;
            }
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public void CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, OnlineMode onlineMode, string ip, int port, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitHost.CanCall())
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Create Lobby hit the rate limit.");
                return;
            }

            string uasId = AuthenticationService.Instance.PlayerId;

            var initialLobbyData = new Dictionary<string, DataObject>()
            {
                {"OnlineMode", new DataObject(DataObject.VisibilityOptions.Public, ((int)onlineMode).ToString())},
                {"IP", new DataObject(DataObject.VisibilityOptions.Public, ip)},
                {"Port", new DataObject(DataObject.VisibilityOptions.Public,  port.ToString())},
            };

            m_LobbyApiInterface.CreateLobbyAsync(uasId, lobbyName, maxPlayers, isPrivate, m_LocalUser.GetDataForUnityServices(), initialLobbyData, onSuccess, onFailure);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public void JoinLobbyAsync(string lobbyId, string lobbyCode, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitJoin.CanCall() ||
                (lobbyId == null && lobbyCode == null))
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Join Lobby hit the rate limit.");
                return;
            }

            string uasId = AuthenticationService.Instance.PlayerId;
            if (!string.IsNullOrEmpty(lobbyCode))
            {
                m_LobbyApiInterface.JoinLobbyAsync_ByCode(uasId, lobbyCode, m_LocalUser.GetDataForUnityServices(), onSuccess, onFailure);
            }
            else
            {
                m_LobbyApiInterface.JoinLobbyAsync_ById(uasId, lobbyId, m_LocalUser.GetDataForUnityServices(), onSuccess, onFailure);
            }
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered onlineMode.
        /// </summary>
        public void QuickJoinLobbyAsync(LobbyUser localUser, Action<Lobby> onSuccess, Action onFailure, List<QueryFilter> filters = null)
        {
            if (!m_RateLimitQuickJoin.CanCall())
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return;
            }

            string uasId = AuthenticationService.Instance.PlayerId;
            m_LobbyApiInterface.QuickJoinLobbyAsync(uasId, filters, m_LocalUser.GetDataForUnityServices(), onSuccess, onFailure);
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public void RetrieveLobbyListAsync(Action<QueryResponse> onSuccess, Action onFailure, List<QueryFilter> filters = null)
        {
            if (!m_RateLimitQuery.CanCall())
            {
                onFailure?.Invoke();
                m_RateLimitQuery.EnqueuePendingOperation(() => { RetrieveLobbyListAsync(onSuccess, onFailure, filters); });
                UnityEngine.Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return;
            }

            m_LobbyApiInterface.QueryAllLobbiesAsync(filters, onSuccess, onFailure);
        }

        private void RetrieveLobbyAsync(string lobbyId, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitQuery.CanCall())
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Retrieve Lobby hit the rate limit.");
                return;
            }
            m_LobbyApiInterface.GetLobbyAsync(lobbyId, onSuccess, onFailure);
        }

        /// <summary>
        /// Attempt to leave a lobby, and then delete it if no players remain.
        /// </summary>
        public void LeaveLobbyAsync(string lobbyId, Action onSuccess, Action onFailure)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            m_LobbyApiInterface.LeaveLobbyAsync(uasId, lobbyId, onSuccess, onFailure);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public void UpdatePlayerDataAsync(Dictionary<string, PlayerDataObject> data, Action onSuccess, Action onFailure)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerDataAsync(data, onSuccess, onFailure); }, onSuccess, false))
            {
                return;
            }

            string playerId = AuthenticationService.Instance.PlayerId;

            m_LobbyApiInterface.UpdatePlayerAsync(m_lastKnownLobby.Id, playerId, data, OnComplete,  onFailure,null, null);

            void OnComplete(Lobby result)
            {
                if (result != null) {
                    m_lastKnownLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                }
                onSuccess?.Invoke();
            }
        }

        /// <summary>
        /// Lobby can be provided info about Relay (or any other remote allocation) so it can add automatic disconnect handling.
        /// </summary>
        public void UpdatePlayerRelayInfoAsync(string allocationId, string connectionInfo, Action onComplete, Action onFailure)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerRelayInfoAsync(allocationId, connectionInfo, onComplete, onFailure); }, onComplete, true)) // Do retry here since the RelayUtpSetup that called this might be destroyed right after this.
            {
                return;
            }

            string playerId = AuthenticationService.Instance.PlayerId;
            m_LobbyApiInterface.UpdatePlayerAsync(m_lastKnownLobby.Id, playerId, new Dictionary<string, PlayerDataObject>(), (_)=>onComplete?.Invoke(), onFailure, allocationId, connectionInfo);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public void UpdateLobbyDataAsync(Dictionary<string, DataObject> data, Action onSuccess, Action onFailure)
        {
            if (!ShouldUpdateData(() => { UpdateLobbyDataAsync(data, onSuccess, onFailure); }, onSuccess, false))
            {
                return;
            }


            var dataCurr = m_lastKnownLobby.Data ?? new Dictionary<string, DataObject>();

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
            var shouldLock = m_LocalLobby.OnlineMode == OnlineMode.UnityRelay && string.IsNullOrEmpty(m_LocalLobby.RelayJoinCode);


            m_LobbyApiInterface.UpdateLobbyAsync(m_lastKnownLobby.Id, dataCurr, shouldLock, OnComplete, onFailure);

            void OnComplete(Lobby result)
            {
                if (result != null)
                {
                    m_lastKnownLobby = result;
                }

                onSuccess?.Invoke();
            }
        }

        /// <summary>
        /// If we are in the middle of another operation, hold onto any pending ones until after that.
        /// If we aren't in a lobby yet, leave it to the caller to decide what to do, since some callers might need to retry and others might not.
        /// </summary>
        private bool ShouldUpdateData(Action caller, Action onComplete, bool shouldRetryIfLobbyNull)
        {
            if (m_RateLimitQuery.IsInCooldown)
            {
                m_RateLimitQuery.EnqueuePendingOperation(caller);
                return false;
            }

            Lobby lobby = m_lastKnownLobby;
            if (lobby == null)
            {
                if (shouldRetryIfLobbyNull)
                {
                    m_RateLimitQuery.EnqueuePendingOperation(caller);
                }

                onComplete?.Invoke();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        public void DoLobbyHeartbeat(float dt)
        {
            m_heartbeatTime += dt;
            if (m_heartbeatTime > k_heartbeatPeriod)
            {
                m_heartbeatTime -= k_heartbeatPeriod;
                m_LobbyApiInterface.HeartbeatPlayerAsync(m_lastKnownLobby.Id);
            }
        }

        public void ForceLeaveLobbyAttempt()
        {
            EndTracking();
            m_LobbyContentHeartbeat.EndTracking();

            if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID))
            {
                LeaveLobbyAsync(m_LocalLobby?.LobbyID, null, null);
            }

            m_LocalUser.ResetState();
            m_LocalLobby.Reset(m_LocalUser);
        }
    }
}
