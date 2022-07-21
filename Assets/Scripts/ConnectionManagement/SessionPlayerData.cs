using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public string PlayerName;
        public int PlayerNumber;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public NetworkGuid AvatarNetworkGuid;
        public int CurrentHitPoints;
        public bool HasCharacterSpawned;

        public SessionPlayerData(ulong clientID, string name, NetworkGuid avatarNetworkGuid, int currentHitPoints = 0, bool isConnected = false, bool hasCharacterSpawned = false)
        {
            ClientID = clientID;
            PlayerName = name;
            PlayerNumber = -1;
            PlayerPosition = Vector3.zero;
            PlayerRotation = Quaternion.identity;
            AvatarNetworkGuid = avatarNetworkGuid;
            CurrentHitPoints = currentHitPoints;
            IsConnected = isConnected;
            HasCharacterSpawned = hasCharacterSpawned;
        }

        public bool IsConnected { get; set; }
        public ulong ClientID { get; set; }

        public void Reinitialize()
        {
            HasCharacterSpawned = false;
        }
    }
}
