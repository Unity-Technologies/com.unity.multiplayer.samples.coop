namespace BossRoom.Scripts.Shared.Infrastructure
{
    public static class MessageChannelDIExtenstions
    {
        public static void BindMessageChannel<TMessage>(this DIScope scope)
        {
            scope.BindAsSingle< MessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>>();
        }

        public static void BindBufferedMessageChannel<TMessage>(this DIScope scope)
        {
            scope.BindAsSingle< BufferedMessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>>();
        }
    }
}
