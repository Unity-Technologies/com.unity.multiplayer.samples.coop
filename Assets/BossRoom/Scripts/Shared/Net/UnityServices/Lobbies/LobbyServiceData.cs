using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Used when displaying the lobby list, to indicate when we are awaiting an updated lobby query.
    /// </summary>
    public enum LobbyQueryState
    {
        Empty,
        Fetching,
        Error,
        Fetched
    }

    /// <summary>
    /// Holds data related to the Lobby service itself - The latest retrieved lobby list, the state of retrieval.
    /// </summary>
    [System.Serializable]
    public class LobbyServiceData : Observed<LobbyServiceData>
    {
        LobbyQueryState m_CurrentState = LobbyQueryState.Empty;

        public LobbyQueryState State
        {
            get { return m_CurrentState; }
            set
            {
                m_CurrentState = value;
                OnChanged(this);
            }
        }

        Dictionary<string, LocalLobby> m_currentLobbies = new Dictionary<string, LocalLobby>();

        /// <summary>
        /// Maps from a lobby's ID to the local representation of it. This allows us to remember which remote lobbies are which LocalLobbies.
        /// Will only trigger if the dictionary is set wholesale. Changes in the size or contents will not trigger OnChanged.
        /// </summary>
        public Dictionary<string, LocalLobby> CurrentLobbies
        {
            get { return m_currentLobbies; }
            set
            {
                m_currentLobbies = value;
                OnChanged(this);
            }
        }

        public override void CopyObserved(LobbyServiceData oldObserved)
        {
            m_currentLobbies = oldObserved.CurrentLobbies;
            OnChanged(this);
        }
    }
}
