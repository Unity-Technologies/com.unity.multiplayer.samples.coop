using Unity.Collections;

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


#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public struct CheatUsedMessage
    {
        FixedString32Bytes m_CheatUsed;
        FixedPlayerName m_CheaterName;

        public string CheatUsed => m_CheatUsed.ToString();
        public string CheaterName => m_CheaterName.ToString();

        public CheatUsedMessage(string cheatUsed, string cheaterName)
        {
            m_CheatUsed = cheatUsed;
            m_CheaterName = cheaterName;
        }
    }
#endif
}
