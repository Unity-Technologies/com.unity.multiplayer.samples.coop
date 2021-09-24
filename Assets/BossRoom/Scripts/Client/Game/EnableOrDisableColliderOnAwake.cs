using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
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
