using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class PrefabSpawner : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Prefab;

        public void Spawn()
        {
            Instantiate(m_Prefab, transform.position, transform.rotation);
        }
    }
}
