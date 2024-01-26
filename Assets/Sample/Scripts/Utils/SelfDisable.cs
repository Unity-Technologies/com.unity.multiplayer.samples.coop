using UnityEngine;

namespace Unity.BossRoom.Utils
{
    /// <summary>
    /// Will Disable this game object once active after the delay duration has passed.
    /// </summary>
    public class SelfDisable : MonoBehaviour
    {
        [SerializeField]
        float m_DisabledDelay;
        float m_DisableTimestamp;

        void Update()
        {
            if (Time.time >= m_DisableTimestamp)
            {
                gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            m_DisableTimestamp = Time.time + m_DisabledDelay;
        }
    }
}
