namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    public static class MessageChannelDIExtensions
    {
        public static void BindMessageChannelInstance<TMessage>(this DIScope scope)
        {
            scope.BindInstanceAsSingle<MessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IMessageChannel<TMessage>>(new MessageChannel<TMessage>());
        }
        public static void BindNetworkedMessageChannelInstance<TMessage>(this DIScope scope, string name, int bufferSize) where TMessage : unmanaged
        {
            scope.BindInstanceAsSingle<NetworkedMessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IMessageChannel<TMessage>>(new NetworkedMessageChannel<TMessage>(name, bufferSize));
        }

        public static void BindBufferedMessageChannelInstance<TMessage>(this DIScope scope)
        {
            scope.BindInstanceAsSingle<BufferedMessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IBufferedMessageChannel<TMessage>>(new BufferedMessageChannel<TMessage>());
        }
    }
}
