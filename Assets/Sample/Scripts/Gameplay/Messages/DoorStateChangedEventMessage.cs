using System;
using Unity.Netcode;

namespace Unity.BossRoom.Gameplay.Messages
{
    public struct DoorStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public bool IsDoorOpen;
    }
}
