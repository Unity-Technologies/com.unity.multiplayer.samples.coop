using System;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct WinStateMessage : INetworkSerializeByMemcpy
    {
        public WinState WinState;

        public WinStateMessage(WinState state)
        {
            WinState = state;
        }
    }

    public enum WinState
    {
        Invalid,
        Win,
        Loss
    }
}
