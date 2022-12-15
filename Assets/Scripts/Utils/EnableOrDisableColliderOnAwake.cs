using UnityEngine;

namespace Unity.BossRoom.Utils
{
    public class EnableOrDisableColliderOnAwake : MonoBehaviour
    {
        [SerializeField]
        Collider m_Collider;

        [SerializeField]
        bool m_EnableStateOnAwake;

        void Awake()
        {
            if (m_Collider != null)
                m_Collider.enabled = m_EnableStateOnAwake;
        }
    }
}
