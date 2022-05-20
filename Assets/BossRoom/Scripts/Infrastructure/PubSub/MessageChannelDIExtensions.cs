using Unity.Netcode;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    public static class MessageChannelDIExtensions
    {
        public static void BindMessageChannelInstance<TMessage>(this IContainerBuilder builder, Lifetime lifetime)
        {
            builder.Register<MessageChannel<TMessage>>(lifetime).AsImplementedInterfaces();
        }
        
        public static void BindNetworkedMessageChannelInstance<TMessage>(this IContainerBuilder builder, Lifetime lifetime) where TMessage : unmanaged, INetworkSerializeByMemcpy
        {
            builder.Register<NetworkedMessageChannel<TMessage>>(lifetime).AsImplementedInterfaces();
        }

        public static void BindBufferedMessageChannelInstance<TMessage>(this IContainerBuilder builder, Lifetime lifetime)
        {
            builder.Register<BufferedMessageChannel<TMessage>>(lifetime).AsImplementedInterfaces();
        }
    }
}
