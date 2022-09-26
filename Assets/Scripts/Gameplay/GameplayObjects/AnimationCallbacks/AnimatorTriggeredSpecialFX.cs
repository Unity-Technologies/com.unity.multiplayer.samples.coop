using System;
using System.Collections;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Unity.BossRoom.Gameplay.GameplayObjects.AnimationCallbacks
{
    /// <summary>
    /// Instantiates and maintains graphics prefabs and sound effects. They're triggered by entering
    /// (or exiting) specific nodes in an Animator. (Each relevant Animator node must have an
    /// AnimationNodeHook component attached.)
    /// </summary>
    public class AnimatorTriggeredSpecialFX : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Unused by the game and provided only for internal dev comments; put whatever you want here")]
        [TextArea]
        private string DevNotes; // e.g. "this is for the tank class". Documentation for the artists, because all 4 class's AnimatorTriggeredSpecialFX components are on the same GameObject. Can remove later if desired

        [Serializable]
        internal class AnimatorNodeEntryEvent
        {
            [Tooltip("The name of a node in the Animator's state machine.")]
            public string m_AnimatorNodeName;
            [HideInInspector]
            public int m_AnimatorNodeNameHash; // this is maintained via OnValidate() in the editor

            [Header("Particle Prefab")]
            [Tooltip("The prefab that should be instantiated when we enter an Animator node with this name")]
            public SpecialFXGraphic m_Prefab;
            [Tooltip("Wait this many seconds before instantiating the Prefab. (If we leave the animation node before this point, no FX are played.)")]
            public float m_PrefabSpawnDelaySeconds;
            [Tooltip("If we leave the AnimationNode, should we shutdown the fx or let it play out? 0 = never cancel. Any other time = we can cancel up until this amount of time has elapsed... after that, we just let it play out. So a really big value like 9999 effectively means 'always cancel'")]
            public float m_PrefabCanBeAbortedUntilSecs;
            [Tooltip("If the particle should be parented to a specific bone, link that bone here. (If null, plays at character's feet.)")]
            public Transform m_PrefabParent;
            [Tooltip("Prefab will be spawned with this local offset from the parent (Remember, it's a LOCAL offset, so it's affected by the parent transform's scale and rotation!)")]
            public Vector3 m_PrefabParentOffset;
            [Tooltip("Should we disconnect the prefab from the character? (So the prefab's transform has no parent)")]
            public bool m_DeParentPrefab;

            [Header("Sound Effect")]
            [Tooltip("If we want to use a sound effect that's not in the prefab, specify it here")]
            public AudioClip m_SoundEffect;
            [Tooltip("Time (in seconds) before we start playing this sound. If we leave the animation node before this time, no sound plays")]
            public float m_SoundStartDelaySeconds;
            [Tooltip("Relative volume to play at.")]
            public float m_VolumeMultiplier = 1;
            [Tooltip("Should we loop the sound for as long as we're in the animation node?")]
            public bool m_LoopSound = false;
        }
        [SerializeField]
        internal AnimatorNodeEntryEvent[] m_EventsOnNodeEntry;

        /// <summary>
        /// These are the AudioSources we'll use to play sounds. For non-looping sounds we only need one,
        /// but to play multiple looping sounds we need additional AudioSources, since each one can only
        /// play one looping sound at a time.
        /// (These AudioSources are typically on the same GameObject as us, but they don't have to be.)
        /// </summary>
        [SerializeField]
        internal AudioSource[] m_AudioSources;

        /// <summary>
        /// cached reference to our Animator.
        /// </summary>
        [SerializeField]
        private Animator m_Animator;

        /// <summary>
        /// contains the shortNameHash of all the active animation nodes right now
        /// </summary>
        private HashSet<int> m_ActiveNodes = new HashSet<int>();

        [FormerlySerializedAs("m_ClientCharacterVisualization")]
        [SerializeField]
        ClientCharacter m_ClientCharacter;

        private void Awake()
        {
            Debug.Assert(m_AudioSources != null && m_AudioSources.Length > 0, "No AudioSource plugged into AnimatorTriggeredSpecialFX!", gameObject);

            if (!m_ClientCharacter)
            {
                m_ClientCharacter = GetComponentInParent<ClientCharacter>();

                m_Animator = m_ClientCharacter.OurAnimator;
            }
        }

        public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Debug.Assert(m_Animator == animator); // just a sanity check

            m_ActiveNodes.Add(stateInfo.shortNameHash);

            // figure out which of our on-node-enter events (if any) should be triggered, and trigger it
            foreach (var info in m_EventsOnNodeEntry)
            {
                if (info.m_AnimatorNodeNameHash == stateInfo.shortNameHash)
                {
                    if (info.m_Prefab)
                    {
                        StartCoroutine(CoroPlayStateEnterFX(info));
                    }
                    if (info.m_SoundEffect)
                    {
                        StartCoroutine(CoroPlayStateEnterSound(info));
                    }
                }
            }
        }

        // creates and manages the graphics prefab (but not the sound effect) of an on-enter event
        private IEnumerator CoroPlayStateEnterFX(AnimatorNodeEntryEvent eventInfo)
        {
            if (eventInfo.m_PrefabSpawnDelaySeconds > 0)
                yield return new WaitForSeconds(eventInfo.m_PrefabSpawnDelaySeconds);

            if (!m_ActiveNodes.Contains(eventInfo.m_AnimatorNodeNameHash))
                yield break;

            Transform parent = eventInfo.m_PrefabParent != null ? eventInfo.m_PrefabParent : m_ClientCharacter.transform;
            var instantiatedFX = Instantiate(eventInfo.m_Prefab, parent);
            instantiatedFX.transform.localPosition += eventInfo.m_PrefabParentOffset;

            // should we have no parent transform at all? (Note that we're de-parenting AFTER applying
            // the PrefabParent, so that PrefabParent can still be used to determine the initial position/rotation/scale.)
            if (eventInfo.m_DeParentPrefab)
            {
                instantiatedFX.transform.SetParent(null);
            }

            // now we just need to watch and see if we end up needing to prematurely end these new graphics
            if (eventInfo.m_PrefabCanBeAbortedUntilSecs > 0)
            {
                float timeRemaining = eventInfo.m_PrefabCanBeAbortedUntilSecs - eventInfo.m_PrefabSpawnDelaySeconds;
                while (timeRemaining > 0 && instantiatedFX)
                {
                    yield return new WaitForFixedUpdate();
                    timeRemaining -= Time.fixedDeltaTime;
                    if (!m_ActiveNodes.Contains(eventInfo.m_AnimatorNodeNameHash))
                    {
                        // the node we were in has ended! Shut down the FX
                        if (instantiatedFX)
                        {
                            instantiatedFX.Shutdown();
                        }
                    }
                }
            }
        }

        // plays the sound effect of an on-entry event
        private IEnumerator CoroPlayStateEnterSound(AnimatorNodeEntryEvent eventInfo)
        {
            if (eventInfo.m_SoundStartDelaySeconds > 0)
                yield return new WaitForSeconds(eventInfo.m_SoundStartDelaySeconds);

            if (!m_ActiveNodes.Contains(eventInfo.m_AnimatorNodeNameHash))
                yield break;

            if (!eventInfo.m_LoopSound)
            {
                m_AudioSources[0].PlayOneShot(eventInfo.m_SoundEffect, eventInfo.m_VolumeMultiplier);
            }
            else
            {
                AudioSource audioSource = GetAudioSourceForLooping();
                if (!audioSource)
                    yield break; // we're using all our audio sources already. just give up
                audioSource.volume = eventInfo.m_VolumeMultiplier;
                audioSource.loop = true;
                audioSource.clip = eventInfo.m_SoundEffect;
                audioSource.Play();
                while (m_ActiveNodes.Contains(eventInfo.m_AnimatorNodeNameHash) && audioSource.isPlaying)
                {
                    yield return new WaitForFixedUpdate();
                }
                audioSource.Stop();
            }
        }

        /// <summary>
        /// retrieves an available AudioSource that isn't currently playing a looping sound, or null if none are currently available
        /// </summary>
        private AudioSource GetAudioSourceForLooping()
        {
            foreach (var audioSource in m_AudioSources)
            {
                if (audioSource && !audioSource.isPlaying)
                    return audioSource;
            }
            Debug.LogWarning($"{name} doesn't have enough AudioSources to loop all desired sound effects. (Have {m_AudioSources.Length}, need at least 1 more)", gameObject);
            return null;
        }

        public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Debug.Assert(m_Animator == animator); // just a sanity check

            m_ActiveNodes.Remove(stateInfo.shortNameHash);
        }

        /// <summary>
        /// Precomputes the hashed values for the animator-tags we care about.
        /// (This way we don't have to call Animator.StringToHash() at runtime.)
        /// Also auto-initializes variables when possible.
        /// </summary>
        private void OnValidate()
        {
            if (m_EventsOnNodeEntry != null)
            {
                for (int i = 0; i < m_EventsOnNodeEntry.Length; ++i)
                {
                    m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash = Animator.StringToHash(m_EventsOnNodeEntry[i].m_AnimatorNodeName);
                }
            }

            if (m_AudioSources == null || m_AudioSources.Length == 0) // if we have AudioSources handy, plug them in automatically
            {
                m_AudioSources = GetComponents<AudioSource>();
            }
        }

    }


#if UNITY_EDITOR
    /// <summary>
    /// This adds a button in the Inspector. Pressing it validates that all the
    /// animator node names we reference are actually used by our Animator. We
    /// can also show informational messages about problems with the configuration.
    /// </summary>
    [CustomEditor(typeof(AnimatorTriggeredSpecialFX))]
    [CanEditMultipleObjects]
    public class AnimatorTriggeredSpecialFXEditor : UnityEditor.Editor
    {
        private GUIStyle m_ErrorStyle = null;
        public override void OnInspectorGUI()
        {
            // let Unity do all the normal Inspector stuff...
            DrawDefaultInspector();

            // ... then we tack extra stuff on the bottom
            var fx = (AnimatorTriggeredSpecialFX)target;
            if (!HasAudioSource(fx))
            {
                GUILayout.Label("No Audio Sources Connected!", GetErrorStyle());
            }

            if (GUILayout.Button("Validate Node Names"))
            {
                ValidateNodeNames(fx);
            }

            // it's really hard to follow the inspector when there's a lot of these components on the same GameObject... so let's add a bit of whitespace
            EditorGUILayout.Space(50);
        }

        private GUIStyle GetErrorStyle()
        {
            if (m_ErrorStyle == null)
            {
                m_ErrorStyle = new GUIStyle(EditorStyles.boldLabel);
                m_ErrorStyle.normal.textColor = Color.red;
                m_ErrorStyle.fontSize += 5;
            }
            return m_ErrorStyle;
        }

        private bool HasAudioSource(AnimatorTriggeredSpecialFX fx)
        {
            if (fx.m_AudioSources == null)
                return false;
            foreach (var audioSource in fx.m_AudioSources)
            {
                if (audioSource != null)
                    return true;
            }
            return false;
        }

        private void ValidateNodeNames(AnimatorTriggeredSpecialFX fx)
        {
            Animator animator = fx.GetComponent<Animator>();
            if (!animator)
            {
                // should be impossible because we explicitly RequireComponent the Animator
                EditorUtility.DisplayDialog("Error", "No Animator found on this GameObject!?", "OK");
                return;
            }
            if (animator.runtimeAnimatorController == null)
            {
                // perfectly normal user error: they haven't plugged a controller into the Animator
                EditorUtility.DisplayDialog("Error", "The Animator does not have an AnimatorController in it!", "OK");
                return;
            }

            // make sure there aren't any duplicated event entries!
            int totalErrors = 0;
            for (int i = 0; i < fx.m_EventsOnNodeEntry.Length; ++i)
            {
                for (int j = i + 1; j < fx.m_EventsOnNodeEntry.Length; ++j)
                {
                    if (fx.m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash == fx.m_EventsOnNodeEntry[j].m_AnimatorNodeNameHash
                        && fx.m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash != 0
                        && fx.m_EventsOnNodeEntry[i].m_Prefab == fx.m_EventsOnNodeEntry[j].m_Prefab
                        && fx.m_EventsOnNodeEntry[i].m_SoundEffect == fx.m_EventsOnNodeEntry[j].m_SoundEffect)
                    {
                        ++totalErrors;
                        Debug.LogError($"Entries {i} and {j} in EventsOnNodeEntry refer to the same node name ({fx.m_EventsOnNodeEntry[i].m_AnimatorNodeName}) and have the same prefab/sounds! This is probably a copy-paste error.");
                    }
                }
            }

            // create a map of nameHash -> useful debugging information (which we display in the log if there's a problem)
            Dictionary<int, string> usedNames = new Dictionary<int, string>();
            for (int i = 0; i < fx.m_EventsOnNodeEntry.Length; ++i)
            {
                usedNames[fx.m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash] = $"{fx.m_EventsOnNodeEntry[i].m_AnimatorNodeName} (EventsOnNodeEntry index {i})";
            }

            int totalUsedNames = usedNames.Count;

            // now remove all the hashes that are actually used by the controller
            AnimatorController controller = GetAnimatorController(animator);
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    usedNames.Remove(state.state.nameHash);
                }
            }

            // anything that hasn't gotten removed from usedNames isn't actually valid!
            foreach (var hash in usedNames.Keys)
            {
                Debug.LogError("Could not find Animation node named " + usedNames[hash]);
            }
            totalErrors += usedNames.Keys.Count;

            if (totalErrors == 0)
            {
                EditorUtility.DisplayDialog("Success", $"All {totalUsedNames} referenced node names were found in the Animator. No errors found!", "OK!");
            }
            else
            {
                EditorUtility.DisplayDialog("Errors", $"Found {totalErrors} errors. See the log in the Console tab for more information.", "OK");
            }
        }

        /// <summary>
        /// Pulls the AnimatorController out of an Animator. Important: this technique can only work
        /// in the editor. You can never reference an AnimatorController directly at runtime! (It might
        /// seem to work while you're running the game in the editor, but it won't compile when you
        /// try to build a standalone client, because AnimatorController is in an editor-only namespace.)
        /// </summary>
        private AnimatorController GetAnimatorController(Animator animator)
        {
            Debug.Assert(animator); // already pre-checked
            Debug.Assert(animator.runtimeAnimatorController); // already pre-checked

            // we need the AnimatorController, but there's no direct way to retrieve it from the Animator, because
            // at runtime the actual AnimatorController doesn't exist! Only a runtime representation does. (That's why
            // AnimatorController is in the UnityEditor namespace.) But this *isn't* runtime, so when we retrieve the
            // runtime controller, it will actually be a reference to our real AnimatorController.
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                // if it's not an AnimatorController, it must be an AnimatorOverrideController (because those are currently the only two on-disk representations)
                var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                if (overrideController)
                {
                    // override controllers are not allowed to be nested, so the thing it's overriding has to be our real AnimatorController
                    controller = overrideController.runtimeAnimatorController as AnimatorController;
                }
            }
            if (controller == null)
            {
                // It's neither of the two standard disk representations! ... it must be a new Unity feature or a custom variation
                // Either way, we don't know how to get the real AnimatorController out of it, so we have to stop
                throw new System.Exception($"Unrecognized class derived from RuntimeAnimatorController! {animator.runtimeAnimatorController.GetType().FullName}");
            }
            return controller;
        }

    }
#endif
}
