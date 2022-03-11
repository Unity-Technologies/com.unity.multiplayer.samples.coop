using Unity.Services.Core;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure
{
    public struct UnityServiceErrorMessage
    {
        public enum Service
        {
            Authentication,
            Lobby,
        }

        public string Title;
        public string Message;
        public Service AffectedService;
        public RequestFailedException OriginalException;

        public UnityServiceErrorMessage(string title, string message, Service service, RequestFailedException originalException = null)
        {
            Title = title;
            Message = message;
            AffectedService = service;
            OriginalException = originalException;
        }
    }
}
