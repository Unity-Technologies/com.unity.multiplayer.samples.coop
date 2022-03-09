using Unity.Services.Lobbies;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure
{
    public struct LobbyServiceExceptionMessage
    {
        public LobbyServiceException Exception;

        public LobbyServiceExceptionMessage(LobbyServiceException exception)
        {
            Exception = exception;
        }
    }
}
