using UnityEngine;

/// <summary>
/// Used by simple visual FX to self-destruct.
/// </summary>
/// <remarks>
/// Note on mobile performance: on mobile, it's a major perf-hit to constantly create and
/// destroy GameObjects; in mobile games you should use object pooling for your visual FX instead!
/// </remarks>
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
