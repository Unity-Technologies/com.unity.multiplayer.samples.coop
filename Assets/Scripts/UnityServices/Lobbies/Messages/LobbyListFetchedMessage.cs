using System.Collections.Generic;
using Unity.Services.Multiplayer;

namespace Unity.BossRoom.UnityServices.Lobbies
{
    // Note: MultiplayerSDK refactoring
    public struct LobbyListFetchedMessage
    {
        public readonly IList<ISessionInfo>  LocalLobbies;

        public LobbyListFetchedMessage(IList<ISessionInfo>  localLobbies)
        {
            LocalLobbies = localLobbies;
        }
    }
}
