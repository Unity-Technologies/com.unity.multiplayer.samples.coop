using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure
{
    public class RateLimitCooldown
    {
        float m_TimeSinceLastCall = float.MaxValue;
        readonly float m_CooldownTime;
        readonly UpdateRunner m_UpdateRunner;
        Queue<Action> m_PendingOperations = new Queue<Action>();
        Queue<Action> m_NextPendingOperations = new Queue<Action>();

        public void EnqueuePendingOperation(Action action)
        {
            m_NextPendingOperations.Enqueue(action);
        }

        public RateLimitCooldown(float cooldownTime, UpdateRunner updateRunner)
        {
            m_CooldownTime = cooldownTime;
            m_UpdateRunner = updateRunner;
        }

        public bool CanCall => m_TimeSinceLastCall >= m_CooldownTime;

        public void PutOnCooldown()
        {
            m_UpdateRunner.Subscribe(OnUpdate, m_CooldownTime);
            m_TimeSinceLastCall = 0;
        }

        void OnUpdate(float dt)
        {
            while (m_NextPendingOperations.Count > 0)
            {
                m_PendingOperations.Enqueue(m_NextPendingOperations.Dequeue());
            }
            m_TimeSinceLastCall += dt;

            if (CanCall)
            {
                m_UpdateRunner.Unsubscribe(OnUpdate);

                while (m_PendingOperations.Count > 0)
                {
                    m_PendingOperations.Dequeue()?.Invoke(); // Note: If this ends up enqueuing many operations, we might need to batch them and/or ensure they don't all execute at once.
                }
            }
        }
    }
}
