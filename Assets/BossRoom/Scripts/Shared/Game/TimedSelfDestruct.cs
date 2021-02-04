using UnityEngine;

/// <summary>
/// Used by simple visual FX to self-destruct.
/// </summary>
namespace BossRoom
{
    public class TimedSelfDestruct : MonoBehaviour
    {
        [SerializeField]
        private float m_LifespanSeconds;

        private void Start()
        {
            Destroy(gameObject, m_LifespanSeconds);
        }
    }

}
