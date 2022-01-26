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
        private readonly IInstanceResolver m_diScope;

        public LocalLobbyFactory(IInstanceResolver _scope)
        {
            m_diScope = _scope;
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
