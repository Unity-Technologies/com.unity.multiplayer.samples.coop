using UnityEngine;

namespace Unity.BossRoom.Utils
{
    public class PrefabSpawner : MonoBehaviour
    {
        [SerializeField]
        bool m_UseLocalPosition;

        [SerializeField]
        bool m_UseLocalRotation;

        [SerializeField]
        GameObject m_Prefab;

        [SerializeField]
        Vector3 m_CustomPosition;

        [SerializeField]
        Quaternion m_CustomRotation;

        public void Spawn()
        {
            Instantiate(m_Prefab, m_UseLocalPosition ? transform.position : m_CustomPosition, m_UseLocalRotation ? transform.rotation : m_CustomRotation);
        }
    }
}
