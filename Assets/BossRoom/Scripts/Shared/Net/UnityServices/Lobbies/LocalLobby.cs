using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    [Flags] // Some UI elements will want to specify multiple states in which to be active, so this is Flags.
    public enum LobbyState
    {
        Lobby = 1,
        CountDown = 2,
        InGame = 4
    }

    /// <summary>
    /// A local wrapper around a lobby's remote data, with additional functionality for providing that data to UI elements and tracking local player objects.
    /// (The way that the Lobby service handles its data doesn't necessarily match our needs, so we need to map from that to this LocalLobby for use in the sample code.)
    /// </summary>
    [Serializable]
    public class LocalLobby : Observed<LocalLobby>
    {
        private LobbyUserFactory m_userFactory;

        Dictionary<string, LobbyUser> m_LobbyUsers = new Dictionary<string, LobbyUser>();
        public Dictionary<string, LobbyUser> LobbyUsers => m_LobbyUsers;

        [Inject]
        public void InjectDependencies(LobbyUserFactory userFactory)
        {
            m_userFactory = userFactory;
        }

        public LocalLobby()
        {

        }

        #region LocalLobbyData
        public struct LobbyData
        {
            public string LobbyID { get; set; }
            public string LobbyCode { get; set; }
            public string RelayCode { get; set; }
            public string RelayNGOCode { get; set; }
            public string LobbyName { get; set; }
            public bool Private { get; set; }
            public int MaxPlayerCount { get; set; }
            public LobbyState State { get; set; }
            public long State_LastEdit { get; set; }
            public long RelayNGOCode_LastEdit { get; set; }

            public OnlineMode OnlineMode { get; set; }
            public string IP { get; set; }
            public int Port { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayCode = existing.RelayCode;
                RelayNGOCode = existing.RelayNGOCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
                State = existing.State;
                State_LastEdit = existing.State_LastEdit;
                RelayNGOCode_LastEdit = existing.RelayNGOCode_LastEdit;
                OnlineMode = existing.OnlineMode;
                IP = existing.IP;
                Port = existing.Port;
            }

            public LobbyData(string lobbyCode)
            {
                LobbyID = null;
                LobbyCode = lobbyCode;
                RelayCode = null;
                RelayNGOCode = null;
                LobbyName = null;
                Private = false;
                MaxPlayerCount = -1;
                State = LobbyState.Lobby;
                State_LastEdit = 0;
                RelayNGOCode_LastEdit = 0;
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

        ServerAddress m_relayServer;

        /// <summary>Used only for visual output of the Relay connection info. The obfuscated Relay server IP is obtained during allocation in the RelayUtpSetup.</summary>
        public ServerAddress RelayServer
        {
            get => m_relayServer;
            set
            {
                m_relayServer = value;
                OnChanged(this);
            }
        }

        #endregion

        public void AddPlayer(LobbyUser user)
        {
            if (m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogError($"Cant add player {user.DisplayName}({user.ID}) to lobby: {LobbyID} twice");
                return;
            }

            DoAddPlayer(user);
            OnChanged(this);
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

        public string RelayCode
        {
            get => m_data.RelayCode;
            set
            {
                m_data.RelayCode = value;
                OnChanged(this);
            }
        }

        public string RelayNGOCode
        {
            get => m_data.RelayNGOCode;
            set
            {
                m_data.RelayNGOCode = value;
                m_data.RelayNGOCode_LastEdit = DateTime.Now.Ticks;
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

        public LobbyState State
        {
            get => m_data.State;
            set
            {
                m_data.State = value;
                m_data.State_LastEdit = DateTime.Now.Ticks;
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
            // It's possible for the host to edit the lobby in between the time they last pushed lobby data and the time their pull for new lobby data completes.
            // If that happens, the edit will be lost, so instead we maintain the time of last edit to detect that case.
            var pendingState = data.State;

            var pendingNgoCode = data.RelayNGOCode;
            if (m_data.State_LastEdit > data.State_LastEdit)
                pendingState = m_data.State;

            if (m_data.RelayNGOCode_LastEdit > data.RelayNGOCode_LastEdit)
                pendingNgoCode = m_data.RelayNGOCode;
            m_data = data;
            m_data.State = pendingState;

            m_data.RelayNGOCode = pendingNgoCode;

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
                {"RelayCode", new DataObject(DataObject.VisibilityOptions.Public,  RelayCode)},
                {"RelayNGOCode", new DataObject(DataObject.VisibilityOptions.Public, RelayNGOCode)},
                {"State", new DataObject(DataObject.VisibilityOptions.Public, ((int)State).ToString())},
                {"State_LastEdit", new DataObject(DataObject.VisibilityOptions.Public, Data.State_LastEdit.ToString())},
                {"RelayNGOCode_LastEdit", new DataObject(DataObject.VisibilityOptions.Public, Data.RelayNGOCode_LastEdit.ToString())},
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
                info.RelayCode = lobby.Data.ContainsKey("RelayCode") ? lobby.Data["RelayCode"].Value : null; // By providing RelayCode through the lobby data with Member visibility, we ensure a client is connected to the lobby before they could attempt a relay connection, preventing timing issues between them.
                info.RelayNGOCode = lobby.Data.ContainsKey("RelayNGOCode") ? lobby.Data["RelayNGOCode"].Value : null;
                info.State = lobby.Data.ContainsKey("State") ? (LobbyState) int.Parse(lobby.Data["State"].Value) : LobbyState.Lobby;
                info.State_LastEdit = lobby.Data.ContainsKey("State_LastEdit") ? long.Parse(lobby.Data["State_LastEdit"].Value) : 0;
                info.RelayNGOCode_LastEdit = lobby.Data.ContainsKey("RelayNGOCode_LastEdit") ? long.Parse(lobby.Data["RelayNGOCode_LastEdit"].Value) : 0;
                info.OnlineMode = lobby.Data.ContainsKey("OnlineMode") ? (OnlineMode) int.Parse(lobby.Data["OnlineMode"].Value) : OnlineMode.Unset;
                info.IP = lobby.Data.ContainsKey("IP") ? lobby.Data["IP"].Value : string.Empty;
                info.Port =  lobby.Data.ContainsKey("Port") ? int.Parse(lobby.Data["Port"].Value) : 0;
            }
            else
            {
                info.RelayCode = null;
                info.RelayNGOCode = null;
                info.State = LobbyState.Lobby;
                info.State_LastEdit = 0;
                info.RelayNGOCode_LastEdit = 0;
                info.OnlineMode = OnlineMode.Unset;
                info.IP = string.Empty;
                info.Port = 0;
            }

            var lobbyUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in lobby.Players)
            {
                if (player.Data != null)
                {
                    // If we already know about this player and this player is already connected to Relay, don't overwrite things that Relay might be changing.
                    if (player.Data.ContainsKey("UserStatus") == true && int.TryParse(player.Data["UserStatus"].Value, out int status))
                    {
                        if (status > (int)UserStatus.Connecting && LobbyUsers.ContainsKey(player.Id))
                        {
                            lobbyUsers.Add(player.Id, LobbyUsers[player.Id]);
                            continue;
                        }
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the lobby knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                var incomingData = m_userFactory.Create();

                incomingData.IsHost = lobby.HostId.Equals(player.Id);
                incomingData.DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : default;
                incomingData.UserStatus = player.Data?.ContainsKey("UserStatus") == true ? (UserStatus) int.Parse(player.Data["UserStatus"].Value) : UserStatus.Connecting;
                incomingData.ID = player.Id;

                lobbyUsers.Add(incomingData.ID, incomingData);
            }
            CopyObserved(info, lobbyUsers);
        }
    }
}
