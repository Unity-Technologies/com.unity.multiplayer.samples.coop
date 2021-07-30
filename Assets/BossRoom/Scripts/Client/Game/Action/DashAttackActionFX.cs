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
    /// <remarks>
    /// This script moves the character visualization during the "dash", which is normally performed by
    /// <see cref="ClientCharacterVisualization"/>. Since SmoothMove is called before ActionFX.Update, action fx position changes override interpolated positions
    /// </remarks>
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
            TrySetTrigger(Description.Anim);
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
                TrySetTrigger(Description.Anim2);
            }
        }

        public override void Cancel()
        {
            // OtherAnimatorVariable contains the name of the cancelation trigger
            if (!string.IsNullOrEmpty(Description.OtherAnimatorVariable))
            {
                TrySetTrigger(Description.OtherAnimatorVariable);
            }
        }

    }
}
