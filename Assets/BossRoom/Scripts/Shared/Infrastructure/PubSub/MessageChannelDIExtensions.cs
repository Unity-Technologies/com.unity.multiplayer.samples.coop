using System;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    public static class MessageChannelDIExtensions
    {
        public static void BindMessageChannelInstance<TMessage>(this DIScope scope)
        {
            scope.BindInstanceAsSingle<MessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IMessageChannel<TMessage>>(new MessageChannel<TMessage>());
        }
        public static void BindNetworkedMessageChannelInstance<TMessage>(this DIScope scope) where TMessage : unmanaged, INetworkSerializeByMemcpy
        {
            scope.BindInstanceAsSingle<NetworkedMessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IMessageChannel<TMessage>>(new NetworkedMessageChannel<TMessage>());
        }

        public static void BindBufferedMessageChannelInstance<TMessage>(this DIScope scope)
        {
            scope.BindInstanceAsSingle<BufferedMessageChannel<TMessage>, IPublisher<TMessage>, ISubscriber<TMessage>, IBufferedMessageChannel<TMessage>>(new BufferedMessageChannel<TMessage>());
        }
    }
}
