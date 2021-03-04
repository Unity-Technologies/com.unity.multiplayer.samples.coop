using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// This is attached to each node in the animator's state machines that needs to be able to trigger
    /// special effects (particles and sound effects). When an animation node begins or ends, this little script
    /// routes that info to an associated AnimatorTriggeredSpecialFX component, which is responsible for
    /// playing the sounds/particles appropriate for that character.
    /// </summary>
    /// 
    /// <optimizations>
    /// We use the [SharedBetweenAnimators] attribute as an optimization. That means there's only one instance of this
    /// class, and it's shared between ALL the nodes that use it. So we can't really add member variables to this class!
    /// (Unless we want to do a lot of bookkeeping.)
    ///
    /// That's why the code below needs to do an uncached GetComponents() call. Those calls aren't super fast, so this
    /// is a minor performance trade-off in exchange for modest memory reduction (and reduced garbage-collection when
    /// creatures are instantiated/destroyed).
    ///
    /// In some cases, like if your controller switches animation nodes many times per second, this "optimization" could
    /// end up being a de-optimization instead. In that case, just remove the [SharedBetweenAnimators] attribute and
    /// have this class lazily cache its list of potential callee's.
    /// </optimizations>
    /// 
    /// <remarks>
    /// Note that we get a list of ALL the AnimatorTriggeredSpecialFX attached to the Animator's game object, and check
    /// to see which one is enabled. We need this trick for our multi-character graphics prefab: it has multiple 
    /// AnimatorTriggeredSpecialFX for each of the different character classes, and only the relevant one will be
    /// enabled at any given time.
    ///
    /// Also note that we didn't use GetComponentsInChildren() because it's slower than GetComponents(),
    /// so all the AnimatorTriggeredSpecialFX components need to be on the same GameObject as the Animator.
    /// </remarks>
    [SharedBetweenAnimators]
    public class AnimatorNodeHook : StateMachineBehaviour
    {
        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            foreach (var fxController in animator.GetComponents<AnimatorTriggeredSpecialFX>())
            {
                if (fxController && fxController.enabled)
                {
                    fxController.OnStateEnter(animator, stateInfo, layerIndex);
                }
            }
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    
        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            foreach (var fxController in animator.GetComponents<AnimatorTriggeredSpecialFX>())
            {
                if (fxController && fxController.enabled)
                {
                    fxController.OnStateExit(animator, stateInfo, layerIndex);
                }
            }
        }
    }
}
