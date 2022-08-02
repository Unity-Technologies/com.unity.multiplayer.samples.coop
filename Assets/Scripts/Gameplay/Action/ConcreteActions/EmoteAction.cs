using System;
using Unity.Multiplayer.Samples.BossRoom.Server;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    public class EmoteAction : Action
    {
        public EmoteAction(ServerCharacter serverParent, ref ActionRequestData data) : base(serverParent, ref data)
        {
        }

        public override bool OnStart()
        {
            m_ServerParent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
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
                m_ServerParent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }
    }
}
