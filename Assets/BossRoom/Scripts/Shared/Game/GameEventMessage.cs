namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct LifeStateChangedEventMessage
    {
        public LifeState NewLifeState;
        public FixedPlayerName CharacterName;
        public CharacterTypeEnum CharacterType;
    }

    public struct DoorStateChangedEventMessage
    {
        public bool IsDoorOpen;
    }

    public struct ConnectionEventMessage
    {
        public ConnectStatus ConnectStatus;
        public FixedPlayerName PlayerName;
    }
}
