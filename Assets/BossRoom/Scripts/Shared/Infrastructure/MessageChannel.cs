using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    public interface IPublisher<T>
    {
        void Publish(T message);
    }

    public interface ISubscriber<T>
    {
        IDisposable Subscribe(Action<T> handler);
    }

    public class MessageChannel<T> : IDisposable, IPublisher<T>, ISubscriber<T>
    {
        private readonly List<Action<T>> m_MessageHandlers = new List<Action<T>>();

        private readonly Queue<Action> m_PendingHandlers = new Queue<Action>();
        private bool m_IsDisposed;

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                m_MessageHandlers.Clear();
                m_PendingHandlers.Clear();
            }
        }

        public void Publish(T message)
        {
            while (m_PendingHandlers.Count > 0) m_PendingHandlers.Dequeue()?.Invoke();

            foreach (var messageHandler in m_MessageHandlers) messageHandler?.Invoke(message);
        }

        public IDisposable Subscribe(Action<T> handler)
        {
            Assert.IsTrue(!m_MessageHandlers.Contains(handler), "Attempting to subscribe with the same handler more than once");
            m_PendingHandlers.Enqueue(() => { DoSubscribe(handler); });
            var subscription = new Subscription(this, handler);
            return subscription;

            void DoSubscribe(Action<T> _h)
            {
                if (_h != null && !m_MessageHandlers.Contains(_h))
                    m_MessageHandlers.Add(_h);
            }
        }

        private void Unsubscribe(Action<T> handler)
        {
            m_PendingHandlers.Enqueue(() => { DoUnsubscribe(handler); });

            void DoUnsubscribe(Action<T> _h)
            {
                m_MessageHandlers.Remove(_h);
            }
        }

        private class Subscription : IDisposable
        {
            private Action<T> m_Handler;
            private bool m_isDisposed;
            private MessageChannel<T> m_MessageChannel;

            public Subscription(MessageChannel<T> messageChannel, Action<T> handler)
            {
                m_MessageChannel = messageChannel;
                m_Handler = handler;
            }

            public void Dispose()
            {
                if (!m_isDisposed)
                {
                    m_isDisposed = true;

                    if (!m_MessageChannel.m_IsDisposed) m_MessageChannel.Unsubscribe(m_Handler);

                    m_Handler = null;
                    m_MessageChannel = null;
                }
            }
        }
    }
}
