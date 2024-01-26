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
            m_Collider.enabled = m_EnableStateOnAwake;
        }
    }
}
