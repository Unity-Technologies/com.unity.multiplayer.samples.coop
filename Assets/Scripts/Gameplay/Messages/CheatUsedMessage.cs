using System;
using Unity.BossRoom.Utils;
using Unity.Collections;
using Unity.Netcode;

namespace Unity.BossRoom.Gameplay.Messages
{
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
