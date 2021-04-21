using MLAPI;
using MLAPI.Spawning;
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
            m_EndPos = ActionUtils.GetTeleportDestination(m_Parent.transform.position, Data.Position, true);

            base.Start();

            return true;
        }

        public override void OnTeleport()
        {
            // we've been moved to a new spot by the server! This is expected behavior for DashAttack,
            // but we must be careful not to presume that it's DEFINITELY due to DashAttack... some other gameplay
            // mechanism may have teleported us first! So our safest action is to just end this action visualization.
            m_Teleported = true;
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

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }
            if (m_StartedDash)
            {
                m_Parent.StartFollowingNetworkTransform();
            }
        }

    }
}
