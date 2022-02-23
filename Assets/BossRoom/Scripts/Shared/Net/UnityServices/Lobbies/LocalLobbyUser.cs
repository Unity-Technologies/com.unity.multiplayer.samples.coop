using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Data for a local lobby user instance. This will update data and is observed to know when to push local user changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LocalLobbyUser
    {
        public event Action<LocalLobbyUser> Changed;

        public LocalLobbyUser()
        {
            m_Data = new UserData(isHost: false, displayName: null, id: null);
        }

        public struct UserData
        {
            public bool IsHost { get; set; }
            public string DisplayName { get; set; }
            public string ID { get; set; }

            public UserData(bool isHost, string displayName, string id)
            {
                IsHost = isHost;
                DisplayName = displayName;
                ID = id;
            }
        }

        UserData m_Data;

        public void ResetState()
        {
            m_Data = new UserData(false, m_Data.DisplayName, m_Data.ID);
        }

        /// <summary>
        /// Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [Flags]
        public enum UserMembers
        {
            IsHost = 1,
            DisplayName = 2,
            ID = 4,
        }

        UserMembers m_lastChanged;
        public UserMembers LastChanged => m_lastChanged;

        public bool IsHost
        {
            get { return m_Data.IsHost; }
            set
            {
                if (m_Data.IsHost != value)
                {
                    m_Data.IsHost = value;
                    m_lastChanged = UserMembers.IsHost;
                    OnChanged();
                }
            }
        }

        public string DisplayName
        {
            get => m_Data.DisplayName;
            set
            {
                if (m_Data.DisplayName != value)
                {
                    m_Data.DisplayName = value;
                    m_lastChanged = UserMembers.DisplayName;
                    OnChanged();
                }
            }
        }

        public string ID
        {
            get => m_Data.ID;
            set
            {
                if (m_Data.ID != value)
                {
                    m_Data.ID = value;
                    m_lastChanged = UserMembers.ID;
                    OnChanged();
                }
            }
        }


        public void CopyDataFrom(LocalLobbyUser lobby)
        {
            UserData data = lobby.m_Data;
            int lastChanged = // Set flags just for the members that will be changed.
                (m_Data.IsHost == data.IsHost ? 0 : (int) UserMembers.IsHost) |
                (m_Data.DisplayName == data.DisplayName ? 0 : (int) UserMembers.DisplayName) |
                (m_Data.ID == data.ID ? 0 : (int) UserMembers.ID);

            if (lastChanged == 0) // Ensure something actually changed.
            {
                return;
            }

            m_Data = data;
            m_lastChanged = (UserMembers)lastChanged;

            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke(this);
        }

        public Dictionary<string, PlayerDataObject> GetDataForUnityServices() =>
            new Dictionary<string, PlayerDataObject>()
            {
                {"DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, DisplayName)},
            };
    }
}
