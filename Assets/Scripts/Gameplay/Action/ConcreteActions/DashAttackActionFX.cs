using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
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

        public DashAttackActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent) : base(ref data, clientParent) { }

        public override bool OnStartClient()
        {

            base.OnStartClient();

            return true;
        }

        public override bool OnUpdateClient()
        {
            if (m_Dashed) { return ActionConclusion.Stop; } // we're done!

            return ActionConclusion.Continue;
        }
    }
}
