using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure
{
    public class RateLimitCooldown
    {
        private float m_TimeSinceLastCall = float.MaxValue;
        private readonly float m_CooldownTime;
        private readonly UpdateRunner m_SlowUpdate;
        private Queue<Action> m_PendingOperations = new Queue<Action>();

        public void EnqueuePendingOperation(Action action)
        {
            m_PendingOperations.Enqueue(action);
        }

        public RateLimitCooldown(float cooldownTime, UpdateRunner slowUpdate)
        {
            m_CooldownTime = cooldownTime;
            m_SlowUpdate = slowUpdate;
        }

        public bool CanCall => m_TimeSinceLastCall >= m_CooldownTime;

        public void PutOnCooldown()
        {
            m_SlowUpdate.Subscribe(OnUpdate, m_CooldownTime);
            m_TimeSinceLastCall = 0;
        }

        private void OnUpdate(float dt)
        {
            m_TimeSinceLastCall += dt;

            if (CanCall)
            {
                m_SlowUpdate.Unsubscribe(OnUpdate);

                while (m_PendingOperations.Count > 0)
                {
                    m_PendingOperations.Dequeue()?.Invoke(); // Note: If this ends up enqueuing many operations, we might need to batch them and/or ensure they don't all execute at once.
                }
            }
        }
    }
}
