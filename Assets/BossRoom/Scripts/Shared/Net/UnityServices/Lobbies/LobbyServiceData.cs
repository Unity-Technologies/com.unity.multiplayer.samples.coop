using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    public struct LocalLobbiesRefreshedMessage
    {
        public readonly IReadOnlyDictionary<string, LocalLobby> LobbyIDsToLocalLobbies;
        public LocalLobbiesRefreshedMessage(Dictionary<string, LocalLobby> lobbyIDsToLocalLobbies)
        {
            LobbyIDsToLocalLobbies = lobbyIDsToLocalLobbies;
        }
    }

    /// <summary>
    /// Holds data related to the Lobby service itself - The latest retrieved lobby list
    /// </summary>
    [System.Serializable]
    public sealed class LobbyServiceData
    {
        private Dictionary<string, LocalLobby> m_LobbyIDsToLocalLobbies = new Dictionary<string, LocalLobby>();

        /// <summary>
        /// Maps from a lobby's ID to the local representation of it. This allows us to remember which remote lobbies are which LocalLobbies.
        /// Will only trigger if the dictionary is set wholesale. Changes in the size or contents will not trigger OnChanged.
        /// </summary>
        public IReadOnlyDictionary<string, LocalLobby> LobbyIDsToLocalLobbies => m_LobbyIDsToLocalLobbies;

        private IPublisher<LocalLobbiesRefreshedMessage> m_LobbiesRefreshedPub;

        [Inject]
        private LobbyServiceData(IPublisher<LocalLobbiesRefreshedMessage> lobbiesRefreshed)
        {
            m_LobbiesRefreshedPub = lobbiesRefreshed;
        }

        public void FetchedLobbies(Dictionary<string, LocalLobby> newLobbyDict)
        {
            m_LobbyIDsToLocalLobbies = newLobbyDict;
            m_LobbiesRefreshedPub.Publish(new LocalLobbiesRefreshedMessage(m_LobbyIDsToLocalLobbies));
        }
    }
}
