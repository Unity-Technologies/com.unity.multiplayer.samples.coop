namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct LifeStateChangedEventMessage
    {
        public LifeState NewLifeState;
        public FixedPlayerName CharacterName;
        public CharacterTypeEnum CharacterType;

        public static int Size => sizeof(LifeState) + FixedPlayerName.Size + sizeof(CharacterTypeEnum);
    }

    public struct DoorStateChangedEventMessage
    {
        public bool IsDoorOpen;

        public static int Size => sizeof(bool);
    }

    public struct ConnectionEventMessage
    {
        public ConnectStatus ConnectStatus;
        public FixedPlayerName PlayerName;

        public static int Size => sizeof(ConnectStatus) + FixedPlayerName.Size;
    }
}
