namespace BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure
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
