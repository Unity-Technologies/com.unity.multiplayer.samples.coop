using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BossRoom.Visual
{
    /// <summary>
    /// Utility script attached to special-effects prefabs. These prefabs are
    /// used by various ActionFX that need to show special short-lived graphics
    /// such as "charging up" particles, ground path indicators, etc.
    ///
    /// There are two different conceptual "modes":
    /// - keep running until somebody explicitly calls Shutdown() (this is used by Actions with indeterminate durations; set m_AutoShutdownTime to -1)
    /// - automatically call Shutdown() after a fixed amount of time (set m_AutoShutdownTime to the number of seconds)
    ///
    /// Note that whichever mode is used, Shutdown() may be called prematurely by whoever owns this graphic
    /// in the case of aborted actions.
    /// 
    /// Once Shutdown() is called (one way or another), the object self-destructs after the particles end
    /// (or after a specific additional amount of time).
    /// </summary>
    /// 
    /// <remarks>
    /// When a particle system ends, it usually needs to stick around for a little while
    /// to let the last remaining particles finish rendering. Shutdown() turns off particles,
    /// and then self-destructs after the particles are all gone. ParticleSystems can technically
    /// self-destruct on their own after being stopped: see the "Stop Action" field in the
    /// ParticleSystem's inspector. But this script also acts as a way to self-destruct non-particle
    /// graphics, and if you're implementing object pooling (for improved mobile performance), this
    /// class can be refactored to move itself into an object pool instead of self-destructing.
    /// </remarks>
    public class SpecialFXGraphic : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Particles that should be stopped on Shutdown")]
        public List<ParticleSystem> m_ParticleSystemsToTurnOffOnShutdown;

        [SerializeField]
        [Tooltip("If this graphic should automatically Shutdown after a certain time, set it here (in seconds). -1 means no auto-shutdown.")]
        private float m_AutoShutdownTime = -1;

        [SerializeField]
        [Tooltip("After Shutdown, how long before we self-destruct? 0 means no self destruct. -1 means self-destruct after ALL particles have disappeared")]
        private float m_PostShutdownSelfDestructTime = -1;

        // track when Shutdown() is called so we don't try to do it twice
        private bool m_IsShutdown = false;

        // we keep a reference to our self-destruction coroutine in case we need to abort it prematurely
        private Coroutine coroWaitForSelfDestruct = null;

        private void Start()
        {
            if (m_AutoShutdownTime != -1)
            {
                coroWaitForSelfDestruct = StartCoroutine(CoroWaitForSelfDestruct());
            }
        }

        public void Shutdown()
        {
            if (!m_IsShutdown)
            {
                foreach (var particleSystem in m_ParticleSystemsToTurnOffOnShutdown)
                {
                    particleSystem.Stop();
                }

                // now, when and how do we fully destroy ourselves?
                if (m_PostShutdownSelfDestructTime >= 0)
                {
                    // we have a fixed-time, so just destroy ourselves after that time
                    Destroy(gameObject, m_PostShutdownSelfDestructTime);
                }
                else if (m_PostShutdownSelfDestructTime == -1)
                {
                    // special case! It means "keep checking the particles and self-destruct when they're all fully done"
                    StartCoroutine(CoroWaitForParticlesToEnd());
                }

                m_IsShutdown = true;
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

            if (coroWaitForSelfDestruct != null)
            {
                StopCoroutine(coroWaitForSelfDestruct);
            }

            Destroy(gameObject);
            yield break;
        }

        private IEnumerator CoroWaitForSelfDestruct()
        {
            yield return new WaitForSeconds(m_AutoShutdownTime);
            coroWaitForSelfDestruct = null;
            if (!m_IsShutdown)
            {
                Shutdown();
            }
        }

    }


#if UNITY_EDITOR
    /// <summary>
    /// A custom editor that provides a button in the Inspector to auto-add all the
    /// particle systems in a SpecialFXGraphic (so we don't have to manually maintain the list).
    /// </summary>
    [CustomEditor(typeof(SpecialFXGraphic))]
    public class SpecialFXGraphicEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Auto-Add All Particle Systems"))
            {
                AddAllParticleSystems((SpecialFXGraphic)target);
            }
        }

        private void AddAllParticleSystems(SpecialFXGraphic specialFxGraphic)
        {
            specialFxGraphic.m_ParticleSystemsToTurnOffOnShutdown.Clear();
            foreach (var particleSystem in specialFxGraphic.GetComponentsInChildren<ParticleSystem>())
            {
                specialFxGraphic.m_ParticleSystemsToTurnOffOnShutdown.Add(particleSystem);
            }
        }
    }
#endif

}


