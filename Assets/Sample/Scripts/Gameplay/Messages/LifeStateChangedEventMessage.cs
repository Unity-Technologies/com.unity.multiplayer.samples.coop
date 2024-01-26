using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Utils;
using Unity.Netcode;

namespace Unity.BossRoom.Gameplay.Messages
{
    public struct LifeStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public LifeState NewLifeState;
        public CharacterTypeEnum CharacterType;
        public FixedPlayerName CharacterName;
    }
}
