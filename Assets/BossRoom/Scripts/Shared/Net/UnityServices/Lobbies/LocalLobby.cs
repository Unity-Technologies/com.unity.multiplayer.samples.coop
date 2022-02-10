using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// A local wrapper around a lobby's remote data, with additional functionality for providing that data to UI elements and tracking local player objects.
    /// (The way that the Lobby service handles its data doesn't necessarily match our needs, so we need to map from that to this LocalLobby for use in the sample code.)
    /// </summary>
    [Serializable]
    public class LocalLobby : Observed<LocalLobby>
    {

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public static List<LocalLobby> CreateLocalLobbies(QueryResponse response)
        {
            var retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
            {
                retLst.Add(Create(lobby));
            }
            return retLst;
        }

        public static LocalLobby Create(Lobby lobby)
        {
            LocalLobby data = new LocalLobby();
            data.ApplyRemoteData(lobby);
            return data;
        }

        Dictionary<string, LobbyUser> m_LobbyUsers = new Dictionary<string, LobbyUser>();
        public Dictionary<string, LobbyUser> LobbyUsers => m_LobbyUsers;


        public LocalLobby()
        {

        }

        #region LocalLobbyData
        public struct LobbyData
        {
            public string LobbyID { get; set; }
            public string LobbyCode { get; set; }
            public string RelayJoinCode { get; set; }
            public string LobbyName { get; set; }
            public bool Private { get; set; }
            public int MaxPlayerCount { get; set; }

            public OnlineMode OnlineMode { get; set; }
            public string IP { get; set; }
            public int Port { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayJoinCode = existing.RelayJoinCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
                OnlineMode = existing.OnlineMode;
                IP = existing.IP;
                Port = existing.Port;
            }

            public LobbyData(string lobbyCode)
            {
                LobbyID = null;
                LobbyCode = lobbyCode;
                RelayJoinCode = null;
                LobbyName = null;
                Private = false;
                MaxPlayerCount = -1;
                OnlineMode = OnlineMode.Unset;
                IP = string.Empty;
                Port = 0;
            }
        }

        private LobbyData m_data;
        public LobbyData Data
        {
            get { return new LobbyData(m_data); }
        }


        #endregion

        public void AddPlayer(LobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                DoAddPlayer(user);
                OnChanged(this);
            }
        }

        private void DoAddPlayer(LobbyUser user)
        {
            m_LobbyUsers.Add(user.ID, user);
            user.onChanged += OnChangedUser;
        }

        public void RemovePlayer(LobbyUser user)
        {
            DoRemoveUser(user);
            OnChanged(this);
        }

        private void DoRemoveUser(LobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
            user.onChanged -= OnChangedUser;
        }

        private void OnChangedUser(LobbyUser user)
        {
            OnChanged(this);
        }

        public string LobbyID
        {
            get => m_data.LobbyID;
            set
            {
                m_data.LobbyID = value;
                OnChanged(this);
            }
        }

        public string LobbyCode
        {
            get => m_data.LobbyCode;
            set
            {
                m_data.LobbyCode = value;
                OnChanged(this);
            }
        }

        public string RelayJoinCode
        {
            get => m_data.RelayJoinCode;
            set
            {
                m_data.RelayJoinCode = value;
                OnChanged(this);
            }
        }


        public string LobbyName
        {
            get => m_data.LobbyName;
            set
            {
                m_data.LobbyName = value;
                OnChanged(this);
            }
        }

        public bool Private
        {
            get => m_data.Private;
            set
            {
                m_data.Private = value;
                OnChanged(this);
            }
        }

        public int PlayerCount => m_LobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => m_data.MaxPlayerCount;
            set
            {
                m_data.MaxPlayerCount = value;
                OnChanged(this);
            }
        }

        public OnlineMode OnlineMode
        {
            get => m_data.OnlineMode;
            set
            {
                if (m_data.OnlineMode != value)
                {   m_data.OnlineMode = value;
                    OnChanged(this);
                }
            }
        }

        public void CopyObserved(LobbyData data, Dictionary<string, LobbyUser> currUsers)
        {
            m_data = data;

            if (currUsers == null)
                m_LobbyUsers = new Dictionary<string, LobbyUser>();
            else
            {
                List<LobbyUser> toRemove = new List<LobbyUser>();
                foreach (var oldUser in m_LobbyUsers)
                {
                    if (currUsers.ContainsKey(oldUser.Key))
                        oldUser.Value.CopyObserved(currUsers[oldUser.Key]);
                    else
                        toRemove.Add(oldUser.Value);
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var currUser in currUsers)
                {
                    if (!m_LobbyUsers.ContainsKey(currUser.Key))
                        DoAddPlayer(currUser.Value);
                }
            }

            OnChanged(this);
        }

        // This ends up being called from the lobby list when we get data about a lobby without having joined it yet.
        public override void CopyObserved(LocalLobby oldObserved)
        {
            CopyObserved(oldObserved.Data, oldObserved.m_LobbyUsers);
        }

        public Dictionary<string, DataObject> GetDataForUnityServices() =>
            new Dictionary<string, DataObject>()
            {
                {"RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public,  RelayJoinCode)},
                {"OnlineMode", new DataObject(DataObject.VisibilityOptions.Public, ((int)Data.OnlineMode).ToString())},
                {"IP", new DataObject(DataObject.VisibilityOptions.Public, Data.IP)},
                {"Port", new DataObject(DataObject.VisibilityOptions.Public,  Data.Port.ToString())},
            };


        public void ApplyRemoteData(Lobby lobby)
        {
            var info = new LobbyData(); // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.
            info.LobbyID = lobby.Id;
            info.LobbyCode = lobby.LobbyCode;
            info.Private = lobby.IsPrivate;
            info.LobbyName = lobby.Name;
            info.MaxPlayerCount = lobby.MaxPlayers;

            if (lobby.Data != null)
            {
                info.RelayJoinCode = lobby.Data.ContainsKey("RelayJoinCode") ? lobby.Data["RelayJoinCode"].Value : null; // By providing RelayCode through the lobby data with Member visibility, we ensure a client is connected to the lobby before they could attempt a relay connection, preventing timing issues between them.
                info.OnlineMode = lobby.Data.ContainsKey("OnlineMode") ? (OnlineMode) int.Parse(lobby.Data["OnlineMode"].Value) : OnlineMode.Unset;
                info.IP = lobby.Data.ContainsKey("IP") ? lobby.Data["IP"].Value : string.Empty;
                info.Port =  lobby.Data.ContainsKey("Port") ? int.Parse(lobby.Data["Port"].Value) : 0;
            }
            else
            {
                info.RelayJoinCode = null;
                info.OnlineMode = OnlineMode.Unset;
                info.IP = string.Empty;
                info.Port = 0;
            }

            var lobbyUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in lobby.Players)
            {
                if (player.Data != null)
                {
                    if (LobbyUsers.ContainsKey(player.Id))
                    {
                        lobbyUsers.Add(player.Id, LobbyUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the lobby knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                var incomingData = new LobbyUser
                {
                    IsHost = lobby.HostId.Equals(player.Id),
                    DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : default,
                    ID = player.Id
                };

                lobbyUsers.Add(incomingData.ID, incomingData);
            }
            CopyObserved(info, lobbyUsers);
        }

        public void Reset(LobbyUser localUser)
        {
            CopyObserved(new LocalLobby.LobbyData(), new Dictionary<string, LobbyUser>());
            AddPlayer(localUser);
        }
    }
}
