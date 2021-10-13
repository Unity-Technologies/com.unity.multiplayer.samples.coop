using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Visualization of a DashAttackAction. See DashAttackAction.cs for more info.
    ///
    /// From the server's point of view, the dash attack is just a delayed teleport, followed by a melee attack.
    /// But on the client, we visualize this as the character dashing across the screen. The dashing begins after
    /// ExecTimeSeconds have elapsed.
    /// </summary>
    // todo this will need refactoring to show non-interpolated NetworkTransform teleport + VFX to hide teleport
    public class DashAttackActionFX : ActionFX
    {
        private bool m_StartedDash;
        private bool m_Dashed;

        public DashAttackActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        public override bool Start()
        {
            if (!Anticipated)
            {
                PlayStartAnim();
            }

            base.Start();

            return true;
        }

        private void PlayStartAnim()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
        }

        public override void AnticipateAction()
        {
            base.AnticipateAction();
            PlayStartAnim();
        }

        public override bool Update()
        {
            if (m_Dashed) { return ActionConclusion.Stop; } // we're done!

            return ActionConclusion.Continue;
        }

        public override void End()
        {
            // Anim2 contains the name of the end-loop-sequence trigger
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }
        }

        public override void Cancel()
        {
            // OtherAnimatorVariable contains the name of the cancelation trigger
            if (!string.IsNullOrEmpty(Description.OtherAnimatorVariable))
            {
                m_Parent.OurAnimator.SetTrigger(Description.OtherAnimatorVariable);
            }
        }
    }
}
