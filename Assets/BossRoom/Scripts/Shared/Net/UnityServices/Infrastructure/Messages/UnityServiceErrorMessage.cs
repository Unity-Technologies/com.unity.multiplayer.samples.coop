namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure
{
    public struct UnityServiceErrorMessage
    {
        public string Title;
        public string Message;
        public UnityServiceErrorMessage(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}
