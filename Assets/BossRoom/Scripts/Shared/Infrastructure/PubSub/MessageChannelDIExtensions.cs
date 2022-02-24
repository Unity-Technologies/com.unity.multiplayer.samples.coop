namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    public static class MessageChannelDIExtensions
    {
        public static void BindMessageChannel<TMessage>(this DIScope scope)
        {
            scope.BindAsSingle< MessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IMessageChannel<TMessage>>();
        }

        public static void BindBufferedMessageChannel<TMessage>(this DIScope scope)
        {
            scope.BindAsSingle< BufferedMessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IBufferedMessageChannel<TMessage>>();
        }
    }
}
