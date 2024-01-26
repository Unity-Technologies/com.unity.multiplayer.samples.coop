using System;

namespace Unity.BossRoom.UnityServices
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
        public Exception OriginalException;

        public UnityServiceErrorMessage(string title, string message, Service service, Exception originalException = null)
        {
            Title = title;
            Message = message;
            AffectedService = service;
            OriginalException = originalException;
        }
    }
}
