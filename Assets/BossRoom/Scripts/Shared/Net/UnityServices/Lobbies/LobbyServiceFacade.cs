using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;

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
        readonly ApplicationController m_ApplicationController;

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
            ApplicationController applicationController,
            UpdateRunner updateRunner,
            LocalLobby localLobby,
            LocalLobbyUser localUser,
            IPublisher<UnityServiceErrorMessage> serviceErrorMessagePub,
            IPublisher<LobbyListFetchedMessage> lobbyListFetchedPub)
        {
            m_ApplicationController = applicationController;
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

            m_RateLimitQuery = new RateLimitCooldown(0f, updateRunner);
            m_RateLimitJoin = new RateLimitCooldown(0f, updateRunner);
            m_RateLimitQuickJoin = new RateLimitCooldown(0f, updateRunner);
            m_RateLimitHost = new RateLimitCooldown(0f, updateRunner);
        }

        public void Dispose()
        {
            EndTracking();
            m_ServiceScope?.Dispose();
        }

        public void BeginTracking(Lobby lobby)
        {
            if(!m_IsTracking)
            {
                m_IsTracking = true;
                CurrentUnityLobby = lobby;
                m_LocalLobby.ApplyRemoteData(lobby);
                // 2s update cadence is arbitrary and is here to demonstrate the fact that this update can be rather infrequent
                // the actual rate limits are tracked via the RateLimitCooldown objects defined above
                m_UpdateRunner.Subscribe(UpdateLobby, 2f);
                m_JoinedLobbyContentHeartbeat.BeginTracking();
            }
        }

        public void EndTracking()
        {
            if (m_IsTracking)
            {
                m_UpdateRunner.Unsubscribe(UpdateLobby);
                m_IsTracking = false;
                m_HeartbeatTime = 0;
                m_JoinedLobbyContentHeartbeat.EndTracking();
                CurrentUnityLobby = null;

                if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID))
                {
                    LeaveLobbyAsync(m_LocalLobby?.LobbyID, null, null);
                }

                m_LocalUser.ResetState();
                m_LocalLobby?.Reset(m_LocalUser);
            }
        }

        void UpdateLobby(float unused)
        {
            RetrieveLobbyAsync(m_LocalLobby.LobbyID, OnSuccess, null);

            void OnSuccess(Lobby lobby)
            {
                CurrentUnityLobby = lobby;
                m_LocalLobby.ApplyRemoteData(lobby);

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
                    ForceLeaveLobbyAttempt();
                    m_ApplicationController.QuitGame();
                }
            }
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public void CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, OnlineMode onlineMode, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitHost.CanCall)
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Create Lobby hit the rate limit.");
                return;
            }

            m_RateLimitHost.PutOnCooldown();

            var initialLobbyData = new Dictionary<string, DataObject>()
            {
                {"OnlineMode", new DataObject(DataObject.VisibilityOptions.Public, ((int)onlineMode).ToString())}
            };

            m_LobbyApiInterface.CreateLobbyAsync(AuthenticationService.Instance.PlayerId, lobbyName, maxPlayers, isPrivate, m_LocalUser.GetDataForUnityServices(), initialLobbyData, onSuccess, onFailure);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public void JoinLobbyAsync(string lobbyId, string lobbyCode, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitJoin.CanCall ||
                (lobbyId == null && lobbyCode == null))
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Join Lobby hit the rate limit.");
                return;
            }
            m_RateLimitJoin.PutOnCooldown();

            if (!string.IsNullOrEmpty(lobbyCode))
            {
                m_LobbyApiInterface.JoinLobbyAsync_ByCode(AuthenticationService.Instance.PlayerId, lobbyCode, m_LocalUser.GetDataForUnityServices(), onSuccess, onFailure);
            }
            else
            {
                m_LobbyApiInterface.JoinLobbyAsync_ById(AuthenticationService.Instance.PlayerId, lobbyId, m_LocalUser.GetDataForUnityServices(), onSuccess, onFailure);
            }
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered onlineMode.
        /// </summary>
        public void QuickJoinLobbyAsync(Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitQuickJoin.CanCall)
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return;
            }

            m_RateLimitQuickJoin.PutOnCooldown();
            m_LobbyApiInterface.QuickJoinLobbyAsync(AuthenticationService.Instance.PlayerId, m_LocalUser.GetDataForUnityServices(), onSuccess, onFailure);
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public void RetrieveLobbyListAsync(Action<QueryResponse> onSuccess, Action onFailure)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return;
            }

            m_RateLimitQuery.PutOnCooldown();
            m_LobbyApiInterface.QueryAllLobbiesAsync(OnSuccess, onFailure);

            void OnSuccess(QueryResponse qr)
            {
                onSuccess?.Invoke(qr);
                m_LobbyListFetchedPub.Publish(new LobbyListFetchedMessage(LocalLobby.CreateLocalLobbies(qr)));
            }
        }

        void RetrieveLobbyAsync(string lobbyId, Action<Lobby> onSuccess, Action onFailure)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Retrieve Lobby hit the rate limit.");
                return;
            }
            m_RateLimitQuery.PutOnCooldown();
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

        /// <summary>
        /// Attempt to push a set of key-value pairs associated with the local player which will overwrite any existing data for these keys.
        /// </summary>
        public void UpdatePlayerDataAsync(Dictionary<string, PlayerDataObject> data, Action onSuccess, Action onFailure)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerDataAsync(data, onSuccess, onFailure); }, onSuccess, false))
            {
                return;
            }

            m_LobbyApiInterface.UpdatePlayerAsync(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, data, OnComplete,  onFailure,null, null);

            void OnComplete(Lobby result)
            {
                if (result != null) {
                    CurrentUnityLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
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

            m_LobbyApiInterface.UpdatePlayerAsync(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, new Dictionary<string, PlayerDataObject>(), (_)=>onComplete?.Invoke(), onFailure, allocationId, connectionInfo);
        }

        /// <summary>
        /// Attempt to update a set of key-value pairs associated with a given lobby.
        /// </summary>
        public void UpdateLobbyDataAsync(Dictionary<string, DataObject> data, Action onSuccess, Action onFailure)
        {
            if (!ShouldUpdateData(() => { UpdateLobbyDataAsync(data, onSuccess, onFailure); }, onSuccess, false))
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
            var shouldLock = m_LocalLobby.OnlineMode == OnlineMode.UnityRelay && string.IsNullOrEmpty(m_LocalLobby.RelayJoinCode);

            m_LobbyApiInterface.UpdateLobbyAsync(CurrentUnityLobby.Id, dataCurr, shouldLock, OnComplete, onFailure);

            void OnComplete(Lobby result)
            {
                if (result != null)
                {
                    CurrentUnityLobby = result;
                }

                onSuccess?.Invoke();
            }
        }

        /// <summary>
        /// If we are in the middle of another operation, hold onto any pending ones until after that.
        /// If we aren't in a lobby yet, leave it to the caller to decide what to do, since some callers might need to retry and others might not.
        /// </summary>
        bool ShouldUpdateData(Action caller, Action onComplete, bool shouldRetryIfLobbyNull)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                m_RateLimitQuery.EnqueuePendingOperation(caller);
                return false;
            }

            if (CurrentUnityLobby == null)
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
            m_HeartbeatTime += dt;
            if (m_HeartbeatTime > k_HeartbeatPeriod)
            {
                m_HeartbeatTime -= k_HeartbeatPeriod;
                m_LobbyApiInterface.HeartbeatPlayerAsync(CurrentUnityLobby.Id);
            }
        }

        public void ForceLeaveLobbyAttempt()
        {
            EndTracking();
        }
    }
}
