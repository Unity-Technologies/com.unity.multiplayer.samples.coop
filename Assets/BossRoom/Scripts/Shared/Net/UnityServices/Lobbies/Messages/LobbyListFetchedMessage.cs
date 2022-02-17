using System.Collections.Generic;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    public struct LobbyListFetchedMessage
    {
        public readonly IReadOnlyList<LocalLobby> LocalLobbies;
        public LobbyListFetchedMessage(List<LocalLobby> localLobbies)
        {
            LocalLobbies = localLobbies;
        }
    }
}
