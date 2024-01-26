using System;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.AnimationCallbacks
{
    /// <summary>
    /// This is attached to each layer in the animator's state machines that needs to be able
    /// to trigger special effects (particles and sound effects). When an animation node begins or ends, this little
    /// script routes that info to an associated AnimatorTriggeredSpecialFX component, which is responsible for
    /// playing the sounds/particles appropriate for that character.
    /// </summary>
    ///
    /// <remarks>
    /// While it's possible to attach this script to individual state-machine nodes, it's more efficient to attach
    /// this script to each Layer in the animator controller -- it will get called for all nodes in that layer.
    ///
    /// Note that we get a list of ALL the AnimatorTriggeredSpecialFX attached to the Animator's game object, and check
    /// to see which one is enabled. We need this trick for our multi-character graphics prefab: it has multiple
    /// AnimatorTriggeredSpecialFX for each of the different character classes, and only the relevant one will be
    /// enabled at any given time.
    /// </remarks>
    public class AnimatorNodeHook : StateMachineBehaviour
    {
        private AnimatorTriggeredSpecialFX[] m_CachedTriggerRefs;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (m_CachedTriggerRefs == null)
                m_CachedTriggerRefs = animator.GetComponentsInChildren<AnimatorTriggeredSpecialFX>();
            foreach (var fxController in m_CachedTriggerRefs)
            {
                if (fxController && fxController.enabled)
                {
                    fxController.OnStateEnter(animator, stateInfo, layerIndex);
                }
            }
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (m_CachedTriggerRefs == null)
                m_CachedTriggerRefs = animator.GetComponentsInChildren<AnimatorTriggeredSpecialFX>();
            foreach (var fxController in m_CachedTriggerRefs)
            {
                if (fxController && fxController.enabled)
                {
                    fxController.OnStateExit(animator, stateInfo, layerIndex);
                }
            }
        }
    }
}
