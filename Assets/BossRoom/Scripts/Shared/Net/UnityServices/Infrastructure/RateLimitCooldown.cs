using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure
{
    public class RateLimitCooldown
    {
        readonly float m_CooldownTimeLength;
        private float m_CooldownFinishedTime;
        readonly UpdateRunner m_UpdateRunner;
        Queue<Action> m_PendingOperations = new Queue<Action>();
        Queue<Action> m_NextPendingOperations = new Queue<Action>();

        public RateLimitCooldown(float cooldownTimeLength, UpdateRunner updateRunner)
        {
            m_CooldownTimeLength = cooldownTimeLength;
            m_CooldownFinishedTime = 0f;
            m_UpdateRunner = updateRunner;
        }

        public void EnqueuePendingOperation(Action action)
        {
            m_PendingOperations.Enqueue(action);
        }

        public bool CanCall => Time.unscaledTime > m_CooldownFinishedTime;

        public bool CanCallSam { get; set; } = true; // todo

        public void PutOnCooldown()
        {
            m_CooldownFinishedTime = Time.unscaledTime + m_CooldownTimeLength;
            m_UpdateRunner.Subscribe(OnUpdate, 1 / 10f);
        }

        // the below should disappear once the SDK handles automatic retries
        void OnUpdate(float dt)
        {
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
