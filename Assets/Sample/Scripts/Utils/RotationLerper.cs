using System;
using UnityEngine;

namespace Unity.BossRoom.Utils
{
    /// <summary>
    /// Utility struct to linearly interpolate between two Quaternion values. Allows for flexible linear interpolations
    /// where current and target change over time.
    /// </summary>
    public struct RotationLerper
    {
        // Calculated start for the most recent interpolation
        Quaternion m_LerpStart;

        // Calculated time elapsed for the most recent interpolation
        float m_CurrentLerpTime;

        // The duration of the interpolation, in seconds
        float m_LerpTime;

        public RotationLerper(Quaternion start, float lerpTime)
        {
            m_LerpStart = start;
            m_CurrentLerpTime = 0f;
            m_LerpTime = lerpTime;
        }

        /// <summary>
        /// Linearly interpolate between two Quaternion values.
        /// </summary>
        /// <param name="current"> Start of the interpolation. </param>
        /// <param name="target"> End of the interpolation. </param>
        /// <returns> A Quaternion value between current and target. </returns>
        public Quaternion LerpRotation(Quaternion current, Quaternion target)
        {
            if (current != target)
            {
                m_LerpStart = current;
                m_CurrentLerpTime = 0f;
            }

            m_CurrentLerpTime += Time.deltaTime;
            if (m_CurrentLerpTime > m_LerpTime)
            {
                m_CurrentLerpTime = m_LerpTime;
            }

            var lerpPercentage = m_CurrentLerpTime / m_LerpTime;

            return Quaternion.Slerp(m_LerpStart, target, lerpPercentage);
        }
    }
}
