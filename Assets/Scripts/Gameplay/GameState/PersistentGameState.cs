using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public enum WinState
    {
        Invalid,
        Win,
        Loss
    }

    /// <summary>
    /// MonoBehaviour containing only one NetworkVariable<WinState> to represent the game session's win state.
    /// </summary>
    public class PersistentGameState : NetworkBehaviour
    {
        public NetworkVariable<WinState> winState = new NetworkVariable<WinState>(WinState.Invalid);

        public static PersistentGameState Instance;
    }
}
