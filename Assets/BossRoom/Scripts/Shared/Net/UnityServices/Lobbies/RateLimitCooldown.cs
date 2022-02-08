using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    public class RateLimitCooldown : Observed<RateLimitCooldown>
    {
        private float m_TimeSinceLastCall = float.MaxValue;
        private readonly float m_CooldownTime;
        private Queue<Action> m_PendingOperations = new Queue<Action>();
        private readonly UpdateRunner m_SlowUpdate;

        public void EnqueuePendingOperation(Action action)
        {
            m_PendingOperations.Enqueue(action);
        }

        private bool m_IsInCooldown = false;

        public bool IsInCooldown
        {
            get => m_IsInCooldown;
            private set
            {
                if (m_IsInCooldown != value)
                {
                    m_IsInCooldown = value;
                    OnChanged(this);
                }
            }
        }

        public RateLimitCooldown(float cooldownTime, UpdateRunner slowUpdate)
        {
            m_CooldownTime = cooldownTime;
            m_SlowUpdate = slowUpdate;
        }

        public bool CanCall()
        {
            if (m_TimeSinceLastCall < m_CooldownTime)
                return false;
            else
            {
                m_SlowUpdate.Subscribe(OnUpdate, m_CooldownTime);
                m_TimeSinceLastCall = 0;
                IsInCooldown = true;
                return true;
            }
        }

        private void OnUpdate(float dt)
        {
            m_TimeSinceLastCall += dt;
            if (m_TimeSinceLastCall >= m_CooldownTime)
            {
                IsInCooldown = false;
                if (!m_IsInCooldown) // It's possible that by setting IsInCooldown, something called CanCall immediately, in which case we want to stay on UpdateSlow.
                {
                    m_SlowUpdate.Unsubscribe(OnUpdate); // Note that this is after IsInCooldown is set, to prevent an Observer from kicking off CanCall again immediately.
                    int numPending = m_PendingOperations.Count; // It's possible a pending operation will re-enqueue itself or new operations, which should wait until the next loop.
                    for (; numPending > 0; numPending--)
                        m_PendingOperations.Dequeue()?.Invoke(); // Note: If this ends up enqueuing many operations, we might need to batch them and/or ensure they don't all execute at once.
                }
            }
        }

        public override void CopyObserved(RateLimitCooldown oldObserved)
        {
            /* This behavior isn't needed; we're just here for the OnChanged event management. */
        }
    }
}
