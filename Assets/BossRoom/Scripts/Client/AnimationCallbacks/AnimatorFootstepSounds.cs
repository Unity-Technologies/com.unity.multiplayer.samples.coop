using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Plays one of a few sound effects, on a loop, based on a variable in an Animator.
    /// We use this to play footstep sounds.
    /// </summary>
    ///
    /// <remarks>
    /// For this project we're using a few looped footstep sounds, choosing between them
    /// based on the animated speed. This method has good performance versus a more complicated
    /// approach, but it does have a flaw: it becomes inaccurate when the character's speed is slowed.
    /// e.g. if a slowness debuff makes you move at 75% speed, the footsteps will be slightly off because
    /// we only have sound-loops for 50% and 100%. That's not a big deal in this particular game, though.
    /// In the rare situations where animated speed is faster than 100% (due to speed buffs etc.), we
    /// currently just don't play any footsteps at all.
    /// </remarks>
    public class AnimatorFootstepSounds : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The Animator we'll track")]
        private Animator m_Animator;

        [SerializeField]
        [Tooltip("A Float parameter on the Animator, with values between 0 (stationary) and 1 (full movement).")]
        private string m_AnimatorVariable;

        [SerializeField]
        [HideInInspector] // this is maintained via OnValidate() in the editor
        private int m_AnimatorVariableHash;

        [SerializeField]
        [Tooltip("The AudioSource we'll use for looped footstep sounds.")]
        private AudioSource m_AudioSource;

        [SerializeField]
        [Tooltip("Loopable audio of the character's footsteps moving at walking speed")]
        private AudioClip m_WalkFootstepAudioClip;

        [SerializeField]
        [Tooltip("Relative volume to play the clip at")]
        private float m_WalkFootstepVolume = 1;

        [SerializeField]
        [Tooltip("Loopable audio of the character's footsteps moving at running speed")]
        private AudioClip m_RunFootstepAudioClip;

        [SerializeField]
        [Tooltip("Relative volume to play the clip at")]
        private float m_RunFootstepVolume = 1;

        [SerializeField]
        [Tooltip("If the speed variable is this or below, we're moving too slowly for footsteps (no sounds played)")]
        private float m_TooSlowThreshold = 0.3f;

        [SerializeField]
        [Tooltip("If the speed variable is between TooSlowThreshold and this, we're walking")]
        private float m_WalkSpeedThreshold = 0.6f;

        [SerializeField]
        [Tooltip("If the speed variable is between WalkSpeedThreshold and this, we're running. (Higher than this means no sound)")]
        private float m_RunSpeedThreshold = 1.2f;

        [SerializeField]
        ClientCharacterVisualization m_ClientCharacterVisualization;

        void Awake()
        {
            // Netcode for GameObjects (Netcode) does not currently support NetworkAnimator binding at runtime. The
            // following is a temporary workaround. Future refactorings will enable this functionality.
            if (!m_Animator && m_ClientCharacterVisualization)
            {
                m_ClientCharacterVisualization.animatorSet += SetAnimator;
            }
        }

        void SetAnimator(Animator animator)
        {
            m_Animator = animator;
        }

        private void Update()
        {
            if (!m_Animator || !m_AudioSource || !m_WalkFootstepAudioClip || !m_RunFootstepAudioClip || m_AnimatorVariableHash == 0)
            {
                // we can't actually run since we don't have the stuff we need. So just stop updating
                enabled = false;
                return;
            }

            // choose which sound effect to use based on how fast we're walking
            AudioClip clipToUse = null;
            float volume = 0;
            float speed = m_Animator.GetFloat(m_AnimatorVariableHash);
            if (speed <= m_TooSlowThreshold)
            {
                // we could have a "VERY slow walk" sound... but we don't, so just play nothing
            }
            else if (speed <= m_WalkSpeedThreshold)
            {
                clipToUse = m_WalkFootstepAudioClip;
                volume = m_WalkFootstepVolume;
            }
            else if (speed <= m_RunSpeedThreshold)
            {
                clipToUse = m_RunFootstepAudioClip;
                volume = m_RunFootstepVolume;
            }
            else
            {
                // we're animating the character's legs faster than either of our clips can support.
                // We could play a faster clip here... but we don't have one, so just play nothing
            }

            // now actually configure and play the appropriate sound
            if (clipToUse == null)
            {
                m_AudioSource.Stop();
                m_AudioSource.clip = null;
            }
            else if (m_AudioSource.clip != clipToUse)
            {
                m_AudioSource.clip = clipToUse;
                m_AudioSource.volume = volume;
                m_AudioSource.loop = true;
                m_AudioSource.Play();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Precomputes the hashed value for the animator-variable we care about.
        /// (This way we don't have to call Animator.StringToHash() at runtime.)
        /// Also auto-initializes variables when possible.
        /// </summary>
        private void OnValidate()
        {
            m_AnimatorVariableHash = Animator.StringToHash(m_AnimatorVariable);

            if (m_AudioSource == null)
                m_AudioSource = GetComponent<AudioSource>();
        }
#endif
    }
}
