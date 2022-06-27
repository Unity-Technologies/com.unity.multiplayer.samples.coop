using System;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class EmoteAction : Action
    {
        public EmoteAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            return false;
        }

        public override bool Update()
        {
            // since we return false at Start(), this method should not execute
            throw new InvalidOperationException("No logic defined.");
        }

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }
    }
}
