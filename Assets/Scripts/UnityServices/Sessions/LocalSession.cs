using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.BossRoom.UnityServices.Sessions
{
    /// <summary>
    /// A local wrapper around a session's remote data, with additional functionality for providing that data to UI
    /// elements and tracking local player objects.
    /// </summary>
    [Serializable]
    public sealed class LocalSession
    {
        Dictionary<string, LocalSessionUser> m_SessionUsers = new();
        public Dictionary<string, LocalSessionUser> sessionUsers => m_SessionUsers;

        SessionData m_Data;

        public event Action<LocalSession> changed;

        public string SessionID
        {
            get => m_Data.SessionID;
            set
            {
                m_Data.SessionID = value;
                OnChanged();
            }
        }

        public string SessionCode
        {
            get => m_Data.SessionCode;
            set
            {
                m_Data.SessionCode = value;
                OnChanged();
            }
        }

        public string RelayJoinCode
        {
            get => m_Data.RelayJoinCode;
            set
            {
                m_Data.RelayJoinCode = value;
                OnChanged();
            }
        }

        public struct SessionData
        {
            public string SessionID { get; set; }
            public string SessionCode { get; set; }
            public string RelayJoinCode { get; set; }
            public string SessionName { get; set; }
            public bool Private { get; set; }
            public int MaxPlayerCount { get; set; }

            public SessionData(SessionData existing)
            {
                SessionID = existing.SessionID;
                SessionCode = existing.SessionCode;
                RelayJoinCode = existing.RelayJoinCode;
                SessionName = existing.SessionName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
            }

            public SessionData(string sessionCode)
            {
                SessionID = null;
                SessionCode = sessionCode;
                RelayJoinCode = null;
                SessionName = null;
                Private = false;
                MaxPlayerCount = -1;
            }
        }

        public void AddUser(LocalSessionUser user)
        {
            if (!m_SessionUsers.ContainsKey(user.ID))
            {
                DoAddUser(user);
                OnChanged();
            }
        }

        void DoAddUser(LocalSessionUser user)
        {
            m_SessionUsers.Add(user.ID, user);
            user.changed += OnChangedUser;
        }

        public void RemoveUser(LocalSessionUser user)
        {
            DoRemoveUser(user);
            OnChanged();
        }

        void DoRemoveUser(LocalSessionUser user)
        {
            if (!m_SessionUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in session: {SessionID}");
                return;
            }

            m_SessionUsers.Remove(user.ID);
            user.changed -= OnChangedUser;
        }

        void OnChangedUser(LocalSessionUser user)
        {
            OnChanged();
        }

        void OnChanged()
        {
            changed?.Invoke(this);
        }

        public void CopyDataFrom(SessionData data, Dictionary<string, LocalSessionUser> currUsers)
        {
            m_Data = data;

            if (currUsers == null)
            {
                m_SessionUsers = new Dictionary<string, LocalSessionUser>();
            }
            else
            {
                List<LocalSessionUser> toRemove = new List<LocalSessionUser>();
                foreach (var oldUser in m_SessionUsers)
                {
                    if (currUsers.ContainsKey(oldUser.Key))
                    {
                        oldUser.Value.CopyDataFrom(currUsers[oldUser.Key]);
                    }
                    else
                    {
                        toRemove.Add(oldUser.Value);
                    }
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var currUser in currUsers)
                {
                    if (!m_SessionUsers.ContainsKey(currUser.Key))
                    {
                        DoAddUser(currUser.Value);
                    }
                }
            }

            OnChanged();
        }

        public Dictionary<string, SessionProperty> GetDataForUnityServices() =>
            new()
            {
                { "RelayJoinCode", new SessionProperty(RelayJoinCode) }
            };

        public void ApplyRemoteData(ISession session)
        {
            var info = new SessionData(); // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.
            info.SessionID = session.Id;
            info.SessionName = session.Name;
            info.MaxPlayerCount = session.MaxPlayers;
            info.SessionCode = session.Code;
            info.Private = session.IsPrivate;

            if (session.Properties != null)
            {
                info.RelayJoinCode = session.Properties.TryGetValue("RelayJoinCode", out var property) ? property.Value : null; // By providing RelayCode through the session properties with Member visibility, we ensure a client is connected to the session before they could attempt a relay connection, preventing timing issues between them.
            }
            else
            {
                info.RelayJoinCode = null;
            }

            var localSessionUsers = new Dictionary<string, LocalSessionUser>();
            foreach (var player in session.Players)
            {
                if (player.Properties != null)
                {
                    if (localSessionUsers.ContainsKey(player.Id))
                    {
                        localSessionUsers.Add(player.Id, localSessionUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the session knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalSession.)
                var incomingData = new LocalSessionUser
                {
                    IsHost = session.Host.Equals(player.Id),
                    DisplayName = player.Properties != null && player.Properties.TryGetValue("DisplayName", out var property) ? property.Value : default,
                    ID = player.Id
                };

                localSessionUsers.Add(incomingData.ID, incomingData);
            }

            CopyDataFrom(info, localSessionUsers);
        }

        public void Reset(LocalSessionUser localUser)
        {
            CopyDataFrom(new SessionData(), new Dictionary<string, LocalSessionUser>());
            AddUser(localUser);
        }
    }
}
