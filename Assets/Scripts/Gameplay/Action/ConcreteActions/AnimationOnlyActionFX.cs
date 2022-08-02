using Unity.Multiplayer.Samples.BossRoom.Visual;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Used for simple Actions that only need to play a few animations (one at startup and optionally
    /// one at end). Lasts a fixed duration as specified in the ActionDescription
    /// </summary>
    public class AnimationOnlyActionFX : ActionFX
    {
        public AnimationOnlyActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent) : base(ref data, clientParent) { }

        public override bool OnUpdateClient()
        {
            return ActionConclusion.Continue;
        }
    }
}
