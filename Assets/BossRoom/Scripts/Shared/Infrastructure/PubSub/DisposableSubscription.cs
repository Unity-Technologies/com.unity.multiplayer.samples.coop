using System;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    /// <summary>
    /// This class is a handle to an active Message Channel subscription and when disposed it unsubscribes from said channel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DisposableSubscription<T> : IDisposable
    {
        private Action<T> m_Handler;
        private bool m_isDisposed;
        private IMessageChannel<T> m_MessageChannel;

        public DisposableSubscription(IMessageChannel<T> messageChannel, Action<T> handler)
        {
            m_MessageChannel = messageChannel;
            m_Handler = handler;
        }

        public void Dispose()
        {
            if (!m_isDisposed)
            {
                m_isDisposed = true;

                if (!m_MessageChannel.IsDisposed)
                {
                    m_MessageChannel.Unsubscribe(m_Handler);
                }

                m_Handler = null;
                m_MessageChannel = null;
            }
        }
    }
}
