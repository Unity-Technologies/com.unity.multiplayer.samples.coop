using System;
using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Current state of the local game.
    /// Set as a flag to allow for the Inspector to select multiple valid states for various UI features.
    /// </summary>
    [Flags]
    public enum GameState
    {
        Menu = 1,
        Lobby = 2,
        JoinMenu = 4,
    }

    /// <summary>
    /// Awaits player input to change the local game data.
    /// </summary>
    [System.Serializable]
    public class LocalGameState : Observed<LocalGameState>
    {
        GameState m_State = GameState.Menu;

        public GameState State
        {
            get => m_State;
            set
            {
                if (m_State != value)
                {
                    m_State = value;
                    OnChanged(this);
                }
            }
        }

        public override void CopyObserved(LocalGameState oldObserved)
        {
            if (m_State == oldObserved.State)
                return;
            m_State = oldObserved.State;
            OnChanged(this);
        }
    }
}
