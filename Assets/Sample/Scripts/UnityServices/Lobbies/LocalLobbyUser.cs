using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Unity.BossRoom.UnityServices.Lobbies
{
    /// <summary>
    /// Data for a local lobby user instance. This will update data and is observed to know when to push local user changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LocalLobbyUser
    {
        public event Action<LocalLobbyUser> changed;

        public LocalLobbyUser()
        {
            m_UserData = new UserData(isHost: false, displayName: null, id: null);
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

        UserData m_UserData;

        public void ResetState()
        {
            m_UserData = new UserData(false, m_UserData.DisplayName, m_UserData.ID);
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

        UserMembers m_LastChanged;

        public bool IsHost
        {
            get { return m_UserData.IsHost; }
            set
            {
                if (m_UserData.IsHost != value)
                {
                    m_UserData.IsHost = value;
                    m_LastChanged = UserMembers.IsHost;
                    OnChanged();
                }
            }
        }

        public string DisplayName
        {
            get => m_UserData.DisplayName;
            set
            {
                if (m_UserData.DisplayName != value)
                {
                    m_UserData.DisplayName = value;
                    m_LastChanged = UserMembers.DisplayName;
                    OnChanged();
                }
            }
        }

        public string ID
        {
            get => m_UserData.ID;
            set
            {
                if (m_UserData.ID != value)
                {
                    m_UserData.ID = value;
                    m_LastChanged = UserMembers.ID;
                    OnChanged();
                }
            }
        }


        public void CopyDataFrom(LocalLobbyUser lobby)
        {
            var data = lobby.m_UserData;
            int lastChanged = // Set flags just for the members that will be changed.
                (m_UserData.IsHost == data.IsHost ? 0 : (int)UserMembers.IsHost) |
                (m_UserData.DisplayName == data.DisplayName ? 0 : (int)UserMembers.DisplayName) |
                (m_UserData.ID == data.ID ? 0 : (int)UserMembers.ID);

            if (lastChanged == 0) // Ensure something actually changed.
            {
                return;
            }

            m_UserData = data;
            m_LastChanged = (UserMembers)lastChanged;

            OnChanged();
        }

        void OnChanged()
        {
            changed?.Invoke(this);
        }

        public Dictionary<string, PlayerDataObject> GetDataForUnityServices() =>
            new Dictionary<string, PlayerDataObject>()
            {
                {"DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, DisplayName)},
            };
    }
}
