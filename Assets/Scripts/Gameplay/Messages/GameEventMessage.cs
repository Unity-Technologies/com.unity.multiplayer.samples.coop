using System;
using Unity.Collections;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct LifeStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public LifeState NewLifeState;
        public CharacterTypeEnum CharacterType;
        public FixedPlayerName CharacterName;
    }

    public struct DoorStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public bool IsDoorOpen;
    }


#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public struct CheatUsedMessage : INetworkSerializeByMemcpy
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
