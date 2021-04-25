using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Visualization of a DashAttackAction. See DashAttackAction.cs for more info.
    /// 
    /// From the server's point of view, the dash attack is just a delayed teleport, followed by a melee attack.
    /// But on the client, we visualize this as the character dashing across the screen. The dashing begins after
    /// ExecTimeSeconds have elapsed.
    /// </summary>
    public class DashAttackActionFX : ActionFX
    {
        private Vector3 m_StartPos;
        private Vector3 m_EndPos;

        private bool m_StartedDash;
        private bool m_Teleported;

        public DashAttackActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        public override bool Start()
        {
            if (!Anticipated)
            {
                PlayStartAnim();
            }

            // save the ending position. But note that we don't save the START position yet!
            m_EndPos = ActionUtils.GetTeleportDestination(m_Parent.transform, Data.Position, true, Description.Range, Description.Range);

            base.Start();

            return true;
        }

        private void PlayStartAnim()
        {
            // this action plays a "melee attack" animation when it successfully ends. But if the player aborts
            // the action, we shouldn't play a swing animation (because we aren't really attacking). To do this,
            // we have a regular trigger to end the animation sequence (in our Anim2 var) and a separate trigger to
            // cancel the animation sequence (in our OtherAnimatorVariable var). Here we just need to make sure that
            // both the cancel-trigger and the end-trigger haven't been left in a raised state from a previous DashAttack.
            // (This isn't strictly necessary but is good anti-bugging. When you have an animation sequence that has multiple
            // end-triggers, it's a good idea to make sure the end-triggers are in the correct state when you start.)
            if (!string.IsNullOrEmpty(Description.OtherAnimatorVariable))
            {
                m_Parent.OurAnimator.ResetTrigger(Description.OtherAnimatorVariable); // reset cancel trigger
            }
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.ResetTrigger(Description.Anim2); // reset end trigger
            }

            // now start the animation sequence
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
        }

        public override void AnticipateAction()
        {
            base.AnticipateAction();
            PlayStartAnim();
        }

        public override bool Update()
        {
            if (m_Teleported) { return ActionConclusion.Stop; } // we're done!

            if (TimeRunning >= Description.ExecTimeSeconds && !m_StartedDash)
            {
                // we've waited for ExecTime, so now we will pretend to dash across the screen.
                m_StartedDash = true;

                // We'll be controlling the visualization for a bit, so tell it to stop auto-moving itself.
                m_Parent.StartFollowingNetworkTransform();

                // Consider our current position to be the starting point of our "dash"
                m_StartPos = m_Parent.transform.position;
            }

            if (m_StartedDash)
            {
                float dashDuration = Description.DurationSeconds - Description.ExecTimeSeconds;
                float dashElapsed = TimeRunning - Description.ExecTimeSeconds;
                Vector3 currentPos = Vector3.Lerp(m_StartPos, m_EndPos, dashElapsed / dashDuration);
                m_Parent.transform.position = currentPos;
            }

            return ActionConclusion.Continue;
        }

        public override void End()
        {
            // Anim2 contains the name of the end-loop-sequence trigger
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }
            if (m_StartedDash)
            {
                m_Parent.StartFollowingNetworkTransform();
            }
        }

        public override void Cancel()
        {
            // OtherAnimatorVariable contains the name of the cancelation trigger
            if (!string.IsNullOrEmpty(Description.OtherAnimatorVariable))
            {
                m_Parent.OurAnimator.SetTrigger(Description.OtherAnimatorVariable);
            }
            if (m_StartedDash)
            {
                m_Parent.StartFollowingNetworkTransform();
            }
        }

    }
}
