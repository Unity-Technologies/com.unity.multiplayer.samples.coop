using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Lobbies.Models;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Data for a local player instance. This will update data and is observed to know when to push local player changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LobbyUser : Observed<LobbyUser>
    {
        public LobbyUser()
        {
            m_data = new UserData(isHost: false, displayName: null, id: null);
        }

        #region Local UserData

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

        private UserData m_data;

        public void ResetState()
        {
            m_data = new UserData(false, m_data.DisplayName, m_data.ID);
        }

        #endregion

        /// <summary>
        /// Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [Flags]
        public enum UserMembers
        {
            IsHost = 1,
            DisplayName = 2,
            ID = 4,
            UserStatus = 8,
            IsApproved = 16
        }

        private UserMembers m_lastChanged;
        public UserMembers LastChanged => m_lastChanged;

        public bool IsHost
        {
            get { return m_data.IsHost; }
            set
            {
                if (m_data.IsHost != value)
                {
                    m_data.IsHost = value;
                    m_lastChanged = UserMembers.IsHost;
                    OnChanged(this);
                }
            }
        }

        public string DisplayName
        {
            get => m_data.DisplayName;
            set
            {
                if (m_data.DisplayName != value)
                {
                    m_data.DisplayName = value;
                    m_lastChanged = UserMembers.DisplayName;
                    OnChanged(this);
                }
            }
        }

        public string ID
        {
            get => m_data.ID;
            set
            {
                if (m_data.ID != value)
                {
                    m_data.ID = value;
                    m_lastChanged = UserMembers.ID;
                    OnChanged(this);
                }
            }
        }


        public override void CopyObserved(LobbyUser observed)
        {
            UserData data = observed.m_data;
            int lastChanged = // Set flags just for the members that will be changed.
                (m_data.IsHost == data.IsHost ? 0 : (int) UserMembers.IsHost) |
                (m_data.DisplayName == data.DisplayName ? 0 : (int) UserMembers.DisplayName) |
                (m_data.ID == data.ID ? 0 : (int) UserMembers.ID);

            if (lastChanged == 0) // Ensure something actually changed.
                return;

            m_data = data;
            m_lastChanged = (UserMembers)lastChanged;

            OnChanged(this);
        }

        public Dictionary<string, PlayerDataObject> GetDataForUnityServices() =>
            new Dictionary<string, PlayerDataObject>()
            {
                {"DisplayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, DisplayName)},
            };
    }
}
