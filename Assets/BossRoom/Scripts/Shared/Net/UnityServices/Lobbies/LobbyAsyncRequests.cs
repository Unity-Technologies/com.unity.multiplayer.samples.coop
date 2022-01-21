using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want. E.g. you can request to get a readable list of
    /// current lobbies and not need to make the query call directly.
    /// </summary>
    public class LobbyAsyncRequests
    {
        [Inject]
        public LobbyAsyncRequests(UpdateSlow slowUpdate, LobbyAPIInterface lobbyAPIInterface, Identity localIdentity)
        {
            m_LocalPlayerIdentity = localIdentity;
            m_LobbyApiInterface = lobbyAPIInterface;
            m_SlowUpdate = slowUpdate;

            m_rateLimitQuery = new RateLimitCooldown(1.5f, slowUpdate); // Used for both the lobby list UI and the in-lobby updating. In the latter case, updates can be cached.
         m_rateLimitJoin = new RateLimitCooldown(3f, slowUpdate);
         m_rateLimitQuickJoin = new RateLimitCooldown(10f,  slowUpdate);
        m_rateLimitHost = new RateLimitCooldown(3f, slowUpdate);

            // Shouldn't need to unsubscribe since this instance won't be replaced. 0.5s is arbitrary; the rate limits are tracked later.
            slowUpdate.Subscribe(UpdateLobby, 0.5f);
        }

        #region Once connected to a lobby, cache the local lobby object so we don't query for it for every lobby operation.

        // (This assumes that the player will be actively in just one lobby at a time, though they could passively be in more.)
        private string m_currentLobbyId = null;
        private Lobby m_lastKnownLobby;
        public Lobby CurrentLobby => m_lastKnownLobby;

        public void BeginTracking(string lobbyId)
        {
            m_currentLobbyId = lobbyId;
        }

        public void EndTracking()
        {
            m_currentLobbyId = null;
            m_lastKnownLobby = null;
            m_heartbeatTime = 0;
        }

        private void UpdateLobby(float unused)
        {
            if (!string.IsNullOrEmpty(m_currentLobbyId))
                RetrieveLobbyAsync(m_currentLobbyId, OnComplete);

            void OnComplete(Unity.Services.Lobbies.Models.Lobby lobby)
            {
                if (lobby != null)
                {
                    m_lastKnownLobby = lobby;
                }
            }
        }

        #endregion

        #region Lobby API calls are rate limited, and some other operations might want an alert when the rate limits have passed.

        // Note that some APIs limit to 1 call per N seconds, while others limit to M calls per N seconds. We'll treat all APIs as though they limited to 1 call per N seconds.
        // Also, this is seralized, so don't reorder the values unless you know what that will affect.
        public enum RequestType
        {
            Query = 0,
            Join,
            QuickJoin,
            Host
        }

        public RateLimitCooldown GetRateLimit(RequestType type)
        {
            if (type == RequestType.Join)
                return m_rateLimitJoin;
            else if (type == RequestType.QuickJoin)
                return m_rateLimitQuickJoin;
            else if (type == RequestType.Host)
                return m_rateLimitHost;
            return m_rateLimitQuery;
        }

        private RateLimitCooldown m_rateLimitQuery; // Used for both the lobby list UI and the in-lobby updating. In the latter case, updates can be cached.
        private RateLimitCooldown m_rateLimitJoin;
        private RateLimitCooldown m_rateLimitQuickJoin;
        private RateLimitCooldown m_rateLimitHost;

        #endregion

        private static Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LobbyUser player)
        {
            Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();
            PlayerDataObject dataObjName = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, player.DisplayName);
            data.Add("DisplayName", dataObjName);
            return data;
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public void CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, LobbyUser localUser, Action<Unity.Services.Lobbies.Models.Lobby> onSuccess, Action onFailure)
        {
            if (!m_rateLimitHost.CanCall())
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Create Lobby hit the rate limit.");
                return;
            }

            string uasId = AuthenticationService.Instance.PlayerId;

            m_LobbyApiInterface.CreateLobbyAsync(uasId, lobbyName, maxPlayers, isPrivate, CreateInitialPlayerData(localUser), OnLobbyCreated);

            void OnLobbyCreated(Unity.Services.Lobbies.Models.Lobby response)
            {
                if (response == null)
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response); // The Create request automatically joins the lobby, so we need not take further action.
            }
        }

        /// <summary>
        /// Attempt to join an existing lobby. Either ID xor code can be null.
        /// </summary>
        public void JoinLobbyAsync(string lobbyId, string lobbyCode, LobbyUser localUser, Action<Unity.Services.Lobbies.Models.Lobby> onSuccess, Action onFailure)
        {
            if (!m_rateLimitJoin.CanCall() ||
                (lobbyId == null && lobbyCode == null))
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Join Lobby hit the rate limit.");
                return;
            }

            string uasId = AuthenticationService.Instance.PlayerId;
            if (!string.IsNullOrEmpty(lobbyId))
                m_LobbyApiInterface.JoinLobbyAsync_ById(uasId, lobbyId, CreateInitialPlayerData(localUser), OnLobbyJoined);
            else
                m_LobbyApiInterface.JoinLobbyAsync_ByCode(uasId, lobbyCode, CreateInitialPlayerData(localUser), OnLobbyJoined);

            void OnLobbyJoined(Unity.Services.Lobbies.Models.Lobby response)
            {
                if (response == null)
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response);
            }
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered limitToColor.
        /// </summary>
        public void QuickJoinLobbyAsync(LobbyUser localUser, LobbyColor limitToColor = LobbyColor.None, Action<Unity.Services.Lobbies.Models.Lobby> onSuccess = null, Action onFailure = null)
        {
            if (!m_rateLimitQuickJoin.CanCall())
            {
                onFailure?.Invoke();
                UnityEngine.Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return;
            }

            var filters = LobbyColorToFilters(limitToColor);
            string uasId = AuthenticationService.Instance.PlayerId;
            m_LobbyApiInterface.QuickJoinLobbyAsync(uasId, filters, CreateInitialPlayerData(localUser), OnLobbyJoined);

            void OnLobbyJoined(Unity.Services.Lobbies.Models.Lobby response)
            {
                if (response == null)
                    onFailure?.Invoke();
                else
                    onSuccess?.Invoke(response);
            }
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        /// <param name="onListRetrieved">If called with null, retrieval was unsuccessful. Else, this will be given a list of contents to display, as pairs of a lobby code and a display string for that lobby.</param>
        public void RetrieveLobbyListAsync(Action<QueryResponse> onListRetrieved, Action<QueryResponse> onError = null, LobbyColor limitToColor = LobbyColor.None)
        {
            if (!m_rateLimitQuery.CanCall())
            {
                onListRetrieved?.Invoke(null);
                m_rateLimitQuery.EnqueuePendingOperation(() => { RetrieveLobbyListAsync(onListRetrieved, onError, limitToColor); });
                UnityEngine.Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return;
            }

            var filters = LobbyColorToFilters(limitToColor);

            m_LobbyApiInterface.QueryAllLobbiesAsync(filters, OnLobbyListRetrieved);

            void OnLobbyListRetrieved(QueryResponse response)
            {
                if (response != null)
                    onListRetrieved?.Invoke(response);
                else
                    onError?.Invoke(response);
            }
        }

        private List<QueryFilter> LobbyColorToFilters(LobbyColor limitToColor)
        {
            List<QueryFilter> filters = new List<QueryFilter>();
            if (limitToColor == LobbyColor.Orange)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Orange).ToString(), QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Green)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Green).ToString(), QueryFilter.OpOptions.EQ));
            else if (limitToColor == LobbyColor.Blue)
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)LobbyColor.Blue).ToString(), QueryFilter.OpOptions.EQ));
            return filters;
        }

        /// <param name="onComplete">If no lobby is retrieved, or if this call hits the rate limit, this is given null.</param>
        private void RetrieveLobbyAsync(string lobbyId, Action<Unity.Services.Lobbies.Models.Lobby> onComplete)
        {
            if (!m_rateLimitQuery.CanCall())
            {
                onComplete?.Invoke(null);
                UnityEngine.Debug.LogWarning("Retrieve Lobby hit the rate limit.");
                return;
            }
            m_LobbyApiInterface.GetLobbyAsync(lobbyId, OnGet);

            void OnGet(Unity.Services.Lobbies.Models.Lobby response)
            {
                onComplete?.Invoke(response); // FUTURE: Consider passing in the exception code here (and elsewhere) to, e.g., specifically handle a 404 indicating a Relay auto-disconnect.
            }
        }

        /// <summary>
        /// Attempt to leave a lobby, and then delete it if no players remain.
        /// </summary>
        /// <param name="onComplete">Called once the request completes, regardless of success or failure.</param>
        public void LeaveLobbyAsync(string lobbyId, Action onComplete)
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            m_LobbyApiInterface.LeaveLobbyAsync(uasId, lobbyId, OnLeftLobby);

            void OnLeftLobby()
            {
                onComplete?.Invoke();

                // Lobbies will automatically delete the lobby if unoccupied, so we don't need to take further action.
            }
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public void UpdatePlayerDataAsync(Dictionary<string, string> data, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerDataAsync(data, onComplete); }, onComplete, false))
                return;

            string playerId = m_LocalPlayerIdentity.GetSubIdentity(IIdentityType.Auth).GetContent("id");
            Dictionary<string, PlayerDataObject> dataCurr = new Dictionary<string, PlayerDataObject>();
            foreach (var dataNew in data)
            {
                PlayerDataObject dataObj = new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: dataNew.Value);
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);
            }

            m_LobbyApiInterface.UpdatePlayerAsync(m_lastKnownLobby.Id, playerId, dataCurr, (result) => {
                if (result != null)
                    m_lastKnownLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                onComplete?.Invoke();
            }, null, null);
        }

        /// <summary>
        /// Lobby can be provided info about Relay (or any other remote allocation) so it can add automatic disconnect handling.
        /// </summary>
        public void UpdatePlayerRelayInfoAsync(string allocationId, string connectionInfo, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdatePlayerRelayInfoAsync(allocationId, connectionInfo, onComplete); }, onComplete, true)) // Do retry here since the RelayUtpSetup that called this might be destroyed right after this.
                return;
            string playerId = m_LocalPlayerIdentity.GetSubIdentity(IIdentityType.Auth).GetContent("id");
            m_LobbyApiInterface.UpdatePlayerAsync(m_lastKnownLobby.Id, playerId, new Dictionary<string, PlayerDataObject>(), (r) => { onComplete?.Invoke(); }, allocationId, connectionInfo);
        }

        /// <param name="data">Key-value pairs, which will overwrite any existing data for these keys. Presumed to be available to all lobby members but not publicly.</param>
        public void UpdateLobbyDataAsync(Dictionary<string, string> data, Action onComplete)
        {
            if (!ShouldUpdateData(() => { UpdateLobbyDataAsync(data, onComplete); }, onComplete, false))
                return;

            Unity.Services.Lobbies.Models.Lobby lobby = m_lastKnownLobby;
            Dictionary<string, DataObject> dataCurr = lobby.Data ?? new Dictionary<string, DataObject>();

			var shouldLock = false;
            foreach (var dataNew in data)
            {
                // Special case: We want to be able to filter on our color data, so we need to supply an arbitrary index to retrieve later. Uses N# for numerics, instead of S# for strings.
                DataObject.IndexOptions index = dataNew.Key == "Color" ? DataObject.IndexOptions.N1 : 0;
                DataObject dataObj = new DataObject(DataObject.VisibilityOptions.Public, dataNew.Value, index); // Public so that when we request the list of lobbies, we can get info about them for filtering.
                if (dataCurr.ContainsKey(dataNew.Key))
                    dataCurr[dataNew.Key] = dataObj;
                else
                    dataCurr.Add(dataNew.Key, dataObj);

                //Special Use: Get the state of the Local lobby so we can lock it from appearing in queries if it's not in the "Lobby" State
                if (dataNew.Key == "State")
                {
                    Enum.TryParse(dataNew.Value, out LobbyState lobbyState);
                    shouldLock = lobbyState != LobbyState.Lobby;
                }
            }

            m_LobbyApiInterface.UpdateLobbyAsync(lobby.Id, dataCurr, shouldLock, (result) =>
            {
                if (result != null)
                    m_lastKnownLobby = result;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// If we are in the middle of another operation, hold onto any pending ones until after that.
        /// If we aren't in a lobby yet, leave it to the caller to decide what to do, since some callers might need to retry and others might not.
        /// </summary>
        private bool ShouldUpdateData(Action caller, Action onComplete, bool shouldRetryIfLobbyNull)
        {
            if (m_rateLimitQuery.IsInCooldown)
            {
                m_rateLimitQuery.EnqueuePendingOperation(caller);
                return false;
            }

            Unity.Services.Lobbies.Models.Lobby lobby = m_lastKnownLobby;
            if (lobby == null)
            {
                if (shouldRetryIfLobbyNull)
                    m_rateLimitQuery.EnqueuePendingOperation(caller);
                onComplete?.Invoke();
                return false;
            }

            return true;
        }

        private float m_heartbeatTime = 0;
        private readonly LobbyAPIInterface m_LobbyApiInterface;
        private readonly UpdateSlow m_SlowUpdate;
        private readonly IIdentity m_LocalPlayerIdentity;
        private const float k_heartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.

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

        public class RateLimitCooldown : Observed<RateLimitCooldown>
        {
            private float m_TimeSinceLastCall = float.MaxValue;
            private readonly float m_CooldownTime;
            private Queue<Action> m_PendingOperations = new Queue<Action>();
            private readonly UpdateSlow m_SlowUpdate;

            public void EnqueuePendingOperation(Action action)
            {
                m_PendingOperations.Enqueue(action);
            }

            private bool m_IsInCooldown = false;

            public bool IsInCooldown
            {
                get => m_IsInCooldown;
                private set
                {
                    if (m_IsInCooldown != value)
                    {
                        m_IsInCooldown = value;
                        OnChanged(this);
                    }
                }
            }

            public RateLimitCooldown(float cooldownTime, UpdateSlow slowUpdate)
            {
                m_CooldownTime = cooldownTime;
            }

            public bool CanCall()
            {
                if (m_TimeSinceLastCall < m_CooldownTime)
                    return false;
                else
                {
                    m_SlowUpdate.Subscribe(OnUpdate, m_CooldownTime);
                    m_TimeSinceLastCall = 0;
                    IsInCooldown = true;
                    return true;
                }
            }

            private void OnUpdate(float dt)
            {
                m_TimeSinceLastCall += dt;
                if (m_TimeSinceLastCall >= m_CooldownTime)
                {
                    IsInCooldown = false;
                    if (!m_IsInCooldown) // It's possible that by setting IsInCooldown, something called CanCall immediately, in which case we want to stay on UpdateSlow.
                    {
                        m_SlowUpdate.Unsubscribe(OnUpdate); // Note that this is after IsInCooldown is set, to prevent an Observer from kicking off CanCall again immediately.
                        int numPending = m_PendingOperations.Count; // It's possible a pending operation will re-enqueue itself or new operations, which should wait until the next loop.
                        for (; numPending > 0; numPending--)
                            m_PendingOperations.Dequeue()?.Invoke(); // Note: If this ends up enqueuing many operations, we might need to batch them and/or ensure they don't all execute at once.
                    }
                }
            }

            public override void CopyObserved(RateLimitCooldown oldObserved)
            {
                /* This behavior isn't needed; we're just here for the OnChanged event management. */
            }
        }
    }
}
