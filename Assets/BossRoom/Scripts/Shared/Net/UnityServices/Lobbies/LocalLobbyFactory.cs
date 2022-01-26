using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using Unity.Services.Lobbies.Models;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Convert the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public class LocalLobbyFactory
    {
        private IInstanceResolver m_diScope;

        [Inject]
        private void InjectDependencies(IInstanceResolver scope)
        {
            m_diScope = scope;
        }

        public LocalLobbyFactory()
        {

        }

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public List<LocalLobby> CreateLocalLobbies(QueryResponse response)
        {
          var retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
            {
                retLst.Add(Create(lobby));
            }
            return retLst;
        }

        private LocalLobby Create(Lobby lobby)
        {
            LocalLobby data = new LocalLobby();
            m_diScope.Inject(data);
            data.ApplyRemoteData(lobby);
            return data;
        }
    }
}
