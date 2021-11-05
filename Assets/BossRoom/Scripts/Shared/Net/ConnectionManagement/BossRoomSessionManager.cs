using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public ulong ClientID;
        public string PlayerName;
        public int PlayerNum;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public NetworkGuid AvatarNetworkGuid;
        public int CurrentHitPoints;
        public bool IsPlayerConnected;
        public bool HasCharacterSpawned;

        public SessionPlayerData(ulong clientID, string name, NetworkGuid avatarNetworkGuid, int currentHitPoints = 0, bool isPlayerConnected = false, bool hasCharacterSpawned = false)
        {
            ClientID = clientID;
            PlayerName = name;
            PlayerNum = -1;
            PlayerPosition = Vector3.zero;
            PlayerRotation = Quaternion.identity;
            AvatarNetworkGuid = avatarNetworkGuid;
            CurrentHitPoints = currentHitPoints;
            IsPlayerConnected = isPlayerConnected;
            HasCharacterSpawned = hasCharacterSpawned;
        }

        public bool IsConnected()
        {
            return IsPlayerConnected;
        }

        public void SetIsConnected(bool isConnected)
        {
            IsPlayerConnected = isConnected;
        }

        public ulong GetClientID()
        {
            return ClientID;
        }

        public void SetClientID(ulong clientID)
        {
            ClientID = clientID;
        }

        public void Reinitialize()
        {
            HasCharacterSpawned = false;
        }
    }

    public class BossRoomSessionManager : SessionManager<SessionPlayerData> { }
}
