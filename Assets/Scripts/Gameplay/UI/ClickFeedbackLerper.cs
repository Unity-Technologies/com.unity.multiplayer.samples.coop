using System;
using Unity.BossRoom.Utils;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.UI
{
    public class ClickFeedbackLerper : MonoBehaviour
    {
        PositionLerper m_PositionLerper;

        Vector3 m_TargetPosition;

        // The amount of offset to keep the click feedback object from intersecting with the floor
        const float k_HoverHeight = 0.15f;
        const float k_LerpTime = 0.04f;

        void Start()
        {
            m_PositionLerper = new PositionLerper(Vector3.zero, k_LerpTime);
        }

        void Update()
        {
            transform.position = m_PositionLerper.LerpPosition(transform.position, m_TargetPosition);
        }

        public void SetTarget(Vector3 clientInputPosition)
        {
            m_TargetPosition.x = clientInputPosition.x;
            m_TargetPosition.y = k_HoverHeight;
            m_TargetPosition.z = clientInputPosition.z;
        }
    }
}
