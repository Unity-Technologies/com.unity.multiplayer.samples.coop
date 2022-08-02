using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Action that plays while a character is Stunned. The character does nothing... just sits there.
    ///
    /// If desired, we can make the character take extra damage from attacks while stunned!
    /// The 'Amount' field of our ActionDescription is used as a multiplier on damage suffered.
    /// (Set it to 1 if you don't want to take more damage while stunned... set it to 2 to take double damage,
    /// or 0.5 to take half damage, etc.)
    /// </summary>
    public class StunnedAction : Action
    {
        public StunnedAction(ref ActionRequestData data) : base(ref data)
        {
        }

        public override bool OnStart(ServerCharacter parent)
        {
            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            return true;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            return true;
        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (buffType == BuffableValue.PercentDamageReceived)
            {
                buffedValue *= Description.Amount;
            }
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }


        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            return ActionConclusion.Continue;
        }
    }
}
