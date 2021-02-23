using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Utility script attached to special-effects prefabs. These prefabs are
    /// used by various ActionFX that need to show special short-lived graphics
    /// such as "charging up" particles, ground path indicators, etc. 
    /// </summary>
    /// <remarks>
    /// When a particle system ends, it usually needs to stick around for a little while
    /// to let the last remaining particles finish rendering. This script turns off particles,
    /// and then self-destructs after the particles are all gone. ParticleSystems can technically
    /// self-destruct on their own after being stopped: see the "Stop Action" field in the
    /// ParticleSystem's inspector. But this script also acts as a way to self-destruct non-particle
    /// graphics, and if you're implementing object pooling (for improved mobile performance), this
    /// class can be refactored to move itself into an object pool instead of self-destructing.
    /// </remarks>
    public class SpecialFXGraphic : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Particles that should be stopped on shutdown")]
        private List<ParticleSystem> m_ParticleSystemsToTurnOffOnShutdown;

        [SerializeField]
        [Tooltip("After shutdown, how long before we self-destruct? 0 means no self destruct. -1 means self-destruct after ALL particles have disappeared")]
        private float m_PostShutdownSelfDestructTime = -1;

        public void Shutdown()
        {
            foreach (var particleSystem in m_ParticleSystemsToTurnOffOnShutdown)
            {
                particleSystem.Stop();
            }

            // now, when and how do we fully destroy ourselves?
            if (m_PostShutdownSelfDestructTime > 0)
            {
                // we have a fixed-time, so just destroy ourselves after that time
                Destroy(gameObject, m_PostShutdownSelfDestructTime);
            }
            else if (m_PostShutdownSelfDestructTime == -1)
            {
                // special case! It means "keep checking the particles and self-destruct when they're all fully done"
                StartCoroutine(CoroWaitForParticlesToEnd());
            }
        }

        private IEnumerator CoroWaitForParticlesToEnd()
        {
            bool foundAliveParticles;
            do
            {
                yield return new WaitForEndOfFrame();
                foundAliveParticles = false;
                foreach (var particleSystem in m_ParticleSystemsToTurnOffOnShutdown)
                {
                    if (particleSystem.IsAlive())
                    {
                        foundAliveParticles = true;
                    }
                }
            } while (foundAliveParticles);
            Destroy(gameObject);
            yield break;
        }

    }

}
