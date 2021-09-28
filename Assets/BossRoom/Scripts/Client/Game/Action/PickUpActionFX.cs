using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// The TargetActionFX runs persistently on the local player, and will attach target reticules to the player's active target.
    /// </summary>
    public class PickUpActionFX : ActionFX
    {
        public PickUpActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent)
        {
        }

        public override bool Start()
        {
            base.Start();

            if (Data.TargetIds != null && Data.TargetIds.Length > 0)
            {
                // pickup
                m_Parent.OurAnimator.SetTrigger(Description.Anim);
            }
            else
            {
                // drop
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }

            return true;
        }

        public override bool Update()
        {
            return true;
        }
    }
}
