using UnityEngine;

/// <summary>
/// Do not use!
///
/// This was used by simple particle-effects prefabs to self-destruct, but those
/// graphics prefabs should now have a SpecialFXGraphic component on them, which
/// controls their lifespan.
///
/// (Leaving this in the tree for now, in case others were already using it.)
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
