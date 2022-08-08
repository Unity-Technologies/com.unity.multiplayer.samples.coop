using System;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{

    [CreateAssetMenu()]
    public class EmoteAction : Action
    {
        public EmoteAction(ref ActionRequestData data)
            : base(ref data) { }

        public override bool OnStart(ServerCharacter parent)
        {
            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            return false;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            // since we return false at Start(), this method should not execute
            throw new InvalidOperationException("No logic defined.");
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            return ActionConclusion.Continue;
        }
    }
}
