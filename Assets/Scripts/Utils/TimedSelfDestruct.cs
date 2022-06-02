using UnityEngine;

/// <summary>
/// Used on short-lived graphical-effect prefabs to self-destruct after a fixed amount of time.
/// Note: this is still safe to use, but is now superceded by SpecialFXGraphic: just set the
/// AutoShutdownTime of that component and it will perform a timed self-destruct.
/// </summary>
/// <remarks>
/// Note that if you're creating an ActionFX that has ANY chance of being prematurely cancelled
/// (such as due to the character being stunned or knocked back or killed), you shouldn't use this.
/// Instead you should use a SpecialFXGraphic component, so the ActionFX can turn off the graphics elegantly.
/// (This lets us turn off particle emissions and wait for the existing ones to end before Destroy()ing.
/// If you Destroy() a particle-system that's actively rendering particles, it can look like a bug to players.)
///
/// Also, performance note: self-destruction is a convenient idiom but not the most performant one. In games
/// for mobile devices (and other lower-graphics-power platforms), it's best to use object pooling instead.
/// </remarks>
namespace Unity.Multiplayer.Samples.BossRoom
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
