using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.BossRoom.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Unity.BossRoom.UnityServices.Sessions
{
    /// <summary>
    /// An abstraction layer between the direct calls into the Multiplayer Services SDK and the outcomes you actually want.
    /// </summary>
    public class MultiplayerServicesFacade : IDisposable, IStartable
    {
        [Inject] LifetimeScope m_ParentScope;
        [Inject] UpdateRunner m_UpdateRunner;
        [Inject] LocalSession m_LocalSession;
        [Inject] LocalSessionUser m_LocalUser;
        [Inject] IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePub;
        [Inject] IPublisher<SessionListFetchedMessage> m_SessionListFetchedPub;

        LifetimeScope m_ServiceScope;
        MultiplayerServicesInterface m_MultiplayerServicesInterface;

        RateLimitCooldown m_RateLimitQuery;
        RateLimitCooldown m_RateLimitJoin;
        RateLimitCooldown m_RateLimitQuickJoin;
        RateLimitCooldown m_RateLimitHost;

        public ISession CurrentUnitySession { get; private set; }

        bool m_IsTracking;

        SessionEventConnectionState m_SessionEventConnectionState = SessionEventConnectionState.Unknown;

        public void Start()
        {
            m_ServiceScope = m_ParentScope.CreateChild(builder =>
            {
                builder.Register<MultiplayerServicesInterface>(Lifetime.Singleton);
            });

            m_MultiplayerServicesInterface = m_ServiceScope.Container.Resolve<MultiplayerServicesInterface>();

            //See https://docs.unity.com/lobby/rate-limits.html
            m_RateLimitQuery = new RateLimitCooldown(1f);
            m_RateLimitJoin = new RateLimitCooldown(3f);
            m_RateLimitQuickJoin = new RateLimitCooldown(10f);
            m_RateLimitHost = new RateLimitCooldown(3f);
        }

        public void Dispose()
        {
            EndTracking();
            if (m_ServiceScope != null)
            {
                m_ServiceScope.Dispose();
            }
        }

        public void SetRemoteSession(ISession session)
        {
            CurrentUnitySession = session;
            m_LocalSession.ApplyRemoteData(session);
        }

        /// <summary>
        /// Initiates tracking of joined session's events. The host also starts sending heartbeat pings here.
        /// </summary>
        public void BeginTracking()
        {
            if (!m_IsTracking)
            {
                m_IsTracking = true;
                SubscribeToJoinedSessionAsync();
            }
        }

        /// <summary>
        /// Ends tracking of joined session's events and leaves or deletes the session. The host also stops sending heartbeat pings here.
        /// </summary>
        public void EndTracking()
        {
            if (m_IsTracking)
            {
                m_IsTracking = false;
                UnsubscribeFromJoinedSessionAsync();
            }

            if (CurrentUnitySession != null)
            {
                if (m_LocalUser.IsHost)
                {
                    DeleteSessionAsync();
                }
                else
                {
                    LeaveSessionAsync();
                }
            }
        }

        /// <summary>
        /// Attempt to create a new session and then join it.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryCreateSessionAsync(string sessionName, int maxPlayers, bool isPrivate)
        {
            if (!m_RateLimitHost.CanCall)
            {
                Debug.LogWarning("Create Session hit the rate limit.");
                return (false, null);
            }

            try
            {
                var session = await m_MultiplayerServicesInterface.CreateSession(AuthenticationService.Instance.PlayerId,
                    sessionName,
                    maxPlayers,
                    isPrivate,
                    m_LocalUser.GetDataForUnityServices(),
                    null);
                return (true, session);
            }
            catch (SessionException e)
            {
                PublishError(e);
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join an existing session. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryJoinSessionAsync(string sessionCode, string sessionId)
        {
            if (!m_RateLimitJoin.CanCall)
            {
                Debug.LogWarning("Join Session hit the rate limit.");
                return (false, null);
            }

            if (sessionId == null && sessionCode == null)
            {
                Debug.LogWarning("Cannot join a Session without a join code or session ID.");
                return (false, null);
            }

            Debug.Log($"Joining session with session code {sessionCode}");
            
            try
            {
                if (!string.IsNullOrEmpty(sessionCode))
                {
                    var session = await m_MultiplayerServicesInterface.JoinSessionByCode(sessionCode, m_LocalUser.GetDataForUnityServices());
                    return (true, session);
                }
                else
                {
                    var session = await m_MultiplayerServicesInterface.JoinSessionById(sessionId, m_LocalUser.GetDataForUnityServices());
                    return (true, session);
                }
            }
            catch (SessionException e)
            {
                PublishError(e);
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join the first session among the available sessions that match the filtered onlineMode.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryQuickJoinSessionAsync()
        {
            if (!m_RateLimitQuickJoin.CanCall)
            {
                Debug.LogWarning("Quick Join Session hit the rate limit.");
                return (false, null);
            }

            try
            {
                var session = await m_MultiplayerServicesInterface.QuickJoinSession(m_LocalUser.GetDataForUnityServices());
                return (true, session);
            }
            catch (SessionException e)
            {
                PublishError(e);
            }

            return (false, null);
        }

        void ResetSession()
        {
            CurrentUnitySession = null;
            m_LocalUser?.ResetState();
            m_LocalSession?.Reset(m_LocalUser);
            // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
        }

        void SubscribeToJoinedSessionAsync()
        {
            CurrentUnitySession.Changed += OnSessionChanged;
            CurrentUnitySession.StateChanged += OnSessionStateChanged;
            CurrentUnitySession.Deleted += OnSessionDeleted;
            CurrentUnitySession.PlayerJoined += OnPlayerJoined;
            CurrentUnitySession.PlayerLeft += OnPlayerLeft;
            CurrentUnitySession.RemovedFromSession += OnRemovedFromSession;
            CurrentUnitySession.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
            CurrentUnitySession.SessionPropertiesChanged += OnSessionPropertiesChanged;
        }
        
        void UnsubscribeFromJoinedSessionAsync()
        {
            CurrentUnitySession.Changed -= OnSessionChanged;
            CurrentUnitySession.StateChanged -= OnSessionStateChanged;
            CurrentUnitySession.Deleted -= OnSessionDeleted;
            CurrentUnitySession.PlayerJoined -= OnPlayerJoined;
            CurrentUnitySession.PlayerLeft -= OnPlayerLeft;
            CurrentUnitySession.RemovedFromSession -= OnRemovedFromSession;
            CurrentUnitySession.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            CurrentUnitySession.SessionPropertiesChanged -= OnSessionPropertiesChanged;
        }
        
        void OnSessionChanged()
        {
            Debug.Log("Session changed.");
            m_LocalSession.ApplyRemoteData(CurrentUnitySession);

            // as client, check if host is still in lobby
            if (!m_LocalUser.IsHost)
            {
                foreach (var lobbyUser in m_LocalSession.sessionUsers)
                {
                    if (lobbyUser.Value.IsHost)
                    {
                        return;
                    }
                }

                m_UnityServiceErrorMessagePub.Publish(new UnityServiceErrorMessage("Host left the session", "Disconnecting.", UnityServiceErrorMessage.Service.Session));
                EndTracking();
                // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
            }
        }
        
        void OnSessionStateChanged(SessionState sessionState)
        {
            switch (sessionState)
            {
                case SessionState.None:
                    break;
                case SessionState.Connected:
                    Debug.Log("Session state changed: Session connected.");
                    break;
                case SessionState.Disconnected:
                    Debug.Log("Session state changed: Session disconnected.");
                    break;
                case SessionState.Deleted:
                    Debug.Log("Session state changed: Session deleted.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sessionState), sessionState, null);
            }
        }
        
        void OnSessionDeleted()
        {
            Debug.Log("Session deleted.");
            ResetSession();
            EndTracking();
        }
        
        void OnPlayerJoined(string playerId)
        {
            Debug.Log($"Player joined: {playerId}");
        }
        
        void OnPlayerLeft(string playerId)
        {
            Debug.Log($"Player left: {playerId}");
        }
        
        void OnRemovedFromSession()
        {
            Debug.Log("Removed from Session.");
            ResetSession();
            EndTracking();
        }
        
        void OnPlayerPropertiesChanged()
        {
            Debug.Log("Player properties changed.");
        }

        void OnSessionPropertiesChanged()
        {
            Debug.Log("Session properties changed.");
        }

        /// <summary>
        /// Used for getting the list of all active sessions, without needing full info for each.
        /// </summary>
        public async Task RetrieveAndPublishSessionListAsync()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                Debug.LogWarning("Retrieving the session list hit the rate limit. Will try again soon...");
                return;
            }

            try
            {
                var queryResults = await m_MultiplayerServicesInterface.QuerySessions();
                m_SessionListFetchedPub.Publish(new SessionListFetchedMessage(queryResults.Sessions));
            }
            catch (SessionException e)
            {
                PublishError(e);
            }
        }

        public async Task<ISession> ReconnectToSessionAsync()
        {
            try
            {
                return await m_MultiplayerServicesInterface.ReconnectToSession(m_LocalSession.SessionID);
            }
            catch (SessionException e)
            {
                // If session is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Error != SessionError.SessionNotFound && !m_LocalUser.IsHost)
                {
                    PublishError(e);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to leave a session
        /// </summary>
        async void LeaveSessionAsync()
        {
            try
            {
                await CurrentUnitySession.LeaveAsync();
            }
            catch (SessionException e)
            {
                // If session is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Error != SessionError.SessionNotFound && !m_LocalUser.IsHost)
                {
                    PublishError(e);
                }
            }
            finally
            {
                ResetSession();
            }

        }

        public async void RemovePlayerFromSessionAsync(string uasId)
        {
            if (m_LocalUser.IsHost)
            {
                try
                {
                    await CurrentUnitySession.AsHost().RemovePlayerAsync(uasId);
                }
                catch (SessionException e)
                {
                    PublishError(e);
                }
            }
            else
            {
                Debug.LogError("Only the host can remove other players from the session.");
            }
        }

        async void DeleteSessionAsync()
        {
            if (m_LocalUser.IsHost)
            {
                try
                {
                    await CurrentUnitySession.AsHost().DeleteAsync();
                }
                catch (SessionException e)
                {
                    PublishError(e);
                }
                finally
                {
                    ResetSession();
                }
            }
            else
            {
                Debug.LogError("Only the host can delete a session.");
            }
        }

        /// <summary>
        /// Attempt to update the set of key-value pairs associated with a given session and unlocks it so clients can see it.
        /// </summary>
        public async Task UpdateSessionPropertiesAndUnlockAsync()
        {
            if (!m_LocalUser.IsHost)
            {
                return;
            }
            
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            var localData = m_LocalSession.GetDataForUnityServices();

            var dataCurr = (Dictionary<string, SessionProperty>)CurrentUnitySession.Properties ?? new Dictionary<string, SessionProperty>();

            foreach (var dataNew in localData)
            {
                dataCurr[dataNew.Key] = dataNew.Value;
            }

            try
            {
                CurrentUnitySession.AsHost().SetProperties(dataCurr);
                await CurrentUnitySession.AsHost().SavePropertiesAsync();
            }
            catch (SessionException e)
            {
                PublishError(e);
            }
        }

        void PublishError(SessionException e)
        {
            if (e.Error == SessionError.RateLimitExceeded)
            {
                m_RateLimitJoin.PutOnCooldown();
                return;
            }
            
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})"; // Session error type, then HTTP error type.
            m_UnityServiceErrorMessagePub.Publish(new UnityServiceErrorMessage("Session Error", reason, UnityServiceErrorMessage.Service.Session, e));
        }
    }
}
