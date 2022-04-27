using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    public class MessageChannel<T> : IMessageChannel<T>
    {
        readonly List<Action<T>> m_MessageHandlers = new List<Action<T>>();

        /// <summary>
        /// This queue of actions that would either add or remove subscriber is used to prevent problems from immediate modification
        /// of the list of subscribers. It could happen if one decides to unsubscribe in a message handler etc.
        /// </summary>
        readonly Queue<Action> m_PendingHandlers = new Queue<Action>();

        public bool IsDisposed { get; private set; } = false;

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                m_MessageHandlers.Clear();
                m_PendingHandlers.Clear();
            }
        }

        public virtual void Publish(T message)
        {
            while (m_PendingHandlers.Count > 0)
            {
                m_PendingHandlers.Dequeue()?.Invoke();
            }

            foreach (var messageHandler in m_MessageHandlers)
            {
                messageHandler?.Invoke(message);
            }
        }

        public virtual IDisposable Subscribe(Action<T> handler)
        {
            Assert.IsTrue(!m_MessageHandlers.Contains(handler), "Attempting to subscribe with the same handler more than once");
           // so we don't modify the handler list while iterating on it (which could happen if the handler unsubscribes). With this, we'd unsubscribe on next publish.
            m_PendingHandlers.Enqueue(() => { DoSubscribe(handler); });

            var subscription = new DisposableSubscription<T>(this, handler);
            return subscription;

            void DoSubscribe(Action<T> h)
            {
                if (h != null && !m_MessageHandlers.Contains(h))
                {
                    m_MessageHandlers.Add(h);
                }
            }
        }

        public void Unsubscribe(Action<T> handler)
        {
            m_PendingHandlers.Enqueue(() => { DoUnsubscribe(handler); });

            void DoUnsubscribe(Action<T> h)
            {
                m_MessageHandlers.Remove(h);
            }
        }
    }
}
