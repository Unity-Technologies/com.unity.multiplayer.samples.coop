using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace BossRoom.Visual
{
    /// <summary>
    /// Instantiates and maintains graphics prefabs and sound effects. They're triggered by entering
    /// (or exiting) specific nodes in an Animator. (Each relevant Animator node must have an
    /// AnimationNodeHook component attached.)
    /// </summary>
    [RequireComponent(typeof(Animator))]
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
            public float m_StartDelaySeconds;
            [Tooltip("If we leave the AnimationNode, should we shutdown the fx or let it play out? 0 = never cancel. Any other time = we can cancel up until this amount of time has elapsed... after that, we just let it play out. So a really big value like 9999 effectively means 'always cancel'")]
            public float m_CanBeAbortedUntilSecs;

            [Header("Sound Effect")]
            [Tooltip("If we want to use a sound effect that's not in the prefab, specify it here")]
            public AudioClip m_SoundEffect;
            [Tooltip("Time (in seconds) before we start playing this sfx. If we leave the node before this time, no sound plays")]
            public float m_SoundStartDelaySeconds;
            [Tooltip("Relative volume to play at.")]
            public float m_VolumeMultiplier = 1;
        }
        [SerializeField]
        internal AnimatorNodeEntryEvent[] m_EventsOnNodeEntry;

        [Serializable]
        internal class AnimatorNodeExitEvent
        {
            [Tooltip("The name of a node in the Animator's state machine.")]
            public string m_AnimatorNodeName;
            [HideInInspector]
            public int m_AnimatorNodeNameHash; // this is maintained via OnValidate() in the editor

            [Header("Particle Prefab")]
            [Tooltip("The prefab that should be instantiated when we exit an AnimatorNode with this name")]
            public SpecialFXGraphic m_Prefab;
            [Tooltip("Wait this many seconds before instantiating the Prefab.")]
            public float m_StartDelaySeconds;

            [Header("Sound Effect")]
            [Tooltip("If we want to use a sound effect that's not in the prefab, specify it here")]
            public AudioClip m_SoundEffect;
            [Tooltip("Time (in seconds) before we start playing this sfx")]
            public float m_SoundStartDelaySeconds;
            [Tooltip("Relative volume to play at.")]
            public float m_VolumeMultiplier = 1;
        }
        [SerializeField]
        internal AnimatorNodeExitEvent[] m_EventsOnNodeExit;

        [SerializeField]
        private AudioSource m_AudioSource;

        /// <summary>
        /// cached reference to our required Animator. (Animator MUST be on the same
        /// GameObject as us so the AnimatorNodeHook can dispatch events to us correctly.)
        /// </summary>
        private Animator m_Animator;

        /// <summary>
        /// contains the shortNameHash of all the active animation nodes right now
        /// </summary>
        private HashSet<int> m_ActiveNodes = new HashSet<int>();

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            Debug.Assert(m_Animator, "AnimatorTriggeredSpecialFX needs to be on the same GameObject as the Animator it works with!", gameObject);
            Debug.Assert(m_AudioSource, "No AudioSource plugged into AnimatorTriggeredSpecialFX!", gameObject);
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
                    break;
                }
            }
        }

        // creates and manages the graphics prefab (but not the sound effect) of an on-enter event
        private IEnumerator CoroPlayStateEnterFX(AnimatorNodeEntryEvent eventInfo)
        {
            if (eventInfo.m_StartDelaySeconds > 0)
                yield return new WaitForSeconds(eventInfo.m_StartDelaySeconds);

            if (!m_ActiveNodes.Contains(eventInfo.m_AnimatorNodeNameHash))
                yield break;

            var instantiatedFX = Instantiate(eventInfo.m_Prefab, m_Animator.transform);

            // now we just need to watch and see if we end up needing to prematurely end these new graphics
            if (eventInfo.m_CanBeAbortedUntilSecs > 0)
            {
                float timeRemaining = eventInfo.m_CanBeAbortedUntilSecs - eventInfo.m_StartDelaySeconds;
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

            m_AudioSource.PlayOneShot(eventInfo.m_SoundEffect, eventInfo.m_VolumeMultiplier);
        }


        public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Debug.Assert(m_Animator == animator); // just a sanity check

            m_ActiveNodes.Remove(stateInfo.shortNameHash);

            // figure out which of our on-node-exit events (if any) should be triggered, and trigger it
            foreach (var info in m_EventsOnNodeExit)
            {
                if (info.m_AnimatorNodeNameHash == stateInfo.shortNameHash)
                {
                    if (info.m_Prefab)
                    {
                        StartCoroutine(CoroPlayStateExitFX(info));
                    }
                    if (info.m_SoundEffect)
                    {
                        StartCoroutine(CoroPlayStateExitSound(info));
                    }
                    break;
                }
            }
        }

        // creates the graphics prefab (but not the sound effect) of an on-exit event
        private IEnumerator CoroPlayStateExitFX(AnimatorNodeExitEvent eventInfo)
        {
            if (eventInfo.m_StartDelaySeconds > 0)
                yield return new WaitForSeconds(eventInfo.m_StartDelaySeconds);

            Instantiate(eventInfo.m_Prefab, m_Animator.transform);
        }

        // plays the sound effect of an on-exit event
        private IEnumerator CoroPlayStateExitSound(AnimatorNodeExitEvent eventInfo)
        {
            if (eventInfo.m_SoundStartDelaySeconds > 0)
                yield return new WaitForSeconds(eventInfo.m_SoundStartDelaySeconds);

            m_AudioSource.PlayOneShot(eventInfo.m_SoundEffect, eventInfo.m_VolumeMultiplier);
        }


#if UNITY_EDITOR
        /// <summary>
        /// Precomputes the hashed values for the animator-tags we care about.
        /// (This way we don't have to call Animator.StringToHash() at runtime.)
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
            if (m_EventsOnNodeExit != null)
            {
                for (int i = 0; i < m_EventsOnNodeExit.Length; ++i)
                {
                    m_EventsOnNodeExit[i].m_AnimatorNodeNameHash = Animator.StringToHash(m_EventsOnNodeExit[i].m_AnimatorNodeName);
                }
            }

            if (!m_AudioSource) // if we have one handy, plug it in
                m_AudioSource = GetComponent<AudioSource>();
            if (!m_AudioSource) // otherwise complain!
                Debug.LogError("AnimatorTriggeredSpecialFX needs an AudioSource to play sound effects through!", gameObject);
        }
#endif

    }


#if UNITY_EDITOR
    /// <summary>
    /// This adds a button in the Inspector. Pressing it validates that all the
    /// animator node names we reference are actually used by our Animator.
    /// </summary>
    [CustomEditor(typeof(AnimatorTriggeredSpecialFX))]
    [CanEditMultipleObjects]
    public class AnimatorTriggeredSpecialFXEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Validate Node Names"))
            {
                ValidateNodeNames((AnimatorTriggeredSpecialFX)target);
            }
            // it's really hard to follow the inspector when there's a lot of these components on the same GameObject... so let's add a bit of whitespace
            GUILayout.Space(50);
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
                for (int j = i+1; j < fx.m_EventsOnNodeEntry.Length; ++j)
                {
                    if (fx.m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash == fx.m_EventsOnNodeEntry[j].m_AnimatorNodeNameHash && fx.m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash != 0)
                    {
                        ++totalErrors;
                        Debug.LogError($"Entries {i} and {j} in EventsOnNodeEntry refer to the same node name ({fx.m_EventsOnNodeEntry[i].m_AnimatorNodeName})! Only the first one will be used.");
                    }
                }
            }

            for (int i = 0; i < fx.m_EventsOnNodeExit.Length; ++i)
            {
                for (int j = i+1; j < fx.m_EventsOnNodeExit.Length; ++j)
                {
                    if (fx.m_EventsOnNodeExit[i].m_AnimatorNodeNameHash == fx.m_EventsOnNodeExit[j].m_AnimatorNodeNameHash && fx.m_EventsOnNodeExit[i].m_AnimatorNodeNameHash != 0)
                    {
                        ++totalErrors;
                        Debug.LogError($"Entries {i} and {j} in EventsOnNodeExit refer to the same node name ({fx.m_EventsOnNodeExit[i].m_AnimatorNodeName})! Only the first one will be used.");
                    }
                }
            }

            // create a map of nameHash -> useful debugging information (which we display in the log if there's a problem)
            Dictionary<int, string> usedNames = new Dictionary<int, string>();
            for (int i = 0; i < fx.m_EventsOnNodeEntry.Length; ++i)
            {
                usedNames[fx.m_EventsOnNodeEntry[i].m_AnimatorNodeNameHash] = $"{fx.m_EventsOnNodeEntry[i].m_AnimatorNodeName} (EventsOnNodeEntry index {i})";
            }
            for (int i = 0; i < fx.m_EventsOnNodeExit.Length; ++i)
            {
                usedNames[fx.m_EventsOnNodeExit[i].m_AnimatorNodeNameHash] = $"{fx.m_EventsOnNodeExit[i].m_AnimatorNodeName} (EventsOnNodeExit index {i})";
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
        /// in the editor. You can never reference an AnimatorController directly at runtime!
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
