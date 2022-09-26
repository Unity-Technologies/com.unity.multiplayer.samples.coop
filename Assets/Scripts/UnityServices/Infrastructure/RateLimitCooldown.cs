using System;
using UnityEngine;

namespace Unity.BossRoom.UnityServices
{
    public class RateLimitCooldown
    {
        public float CooldownTimeLength => m_CooldownTimeLength;

        readonly float m_CooldownTimeLength;
        private float m_CooldownFinishedTime;

        public RateLimitCooldown(float cooldownTimeLength)
        {
            m_CooldownTimeLength = cooldownTimeLength;
            m_CooldownFinishedTime = -1f;
        }

        public bool CanCall => Time.unscaledTime > m_CooldownFinishedTime;

        public void PutOnCooldown()
        {
            m_CooldownFinishedTime = Time.unscaledTime + m_CooldownTimeLength;
        }
    }
}
