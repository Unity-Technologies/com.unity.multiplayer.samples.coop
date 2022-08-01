using System;
using Unity.Multiplayer.Samples.BossRoom.Server;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    public class EmoteAction : Action
    {
        public EmoteAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool OnStart()
        {
            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            return false;
        }

        public override bool OnUpdate()
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
