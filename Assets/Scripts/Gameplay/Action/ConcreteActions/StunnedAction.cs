using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Action that plays while a character is Stunned. The character does nothing... just sits there.
    ///
    /// If desired, we can make the character take extra damage from attacks while stunned!
    /// The 'Amount' field of our ActionDescription is used as a multiplier on damage suffered.
    /// (Set it to 1 if you don't want to take more damage while stunned... set it to 2 to take double damage,
    /// or 0.5 to take half damage, etc.)
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Stunned Action")]
    public class StunnedAction : Action
    {
        public override bool OnStart(ServerCharacter serverCharacter)
        {
            serverCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            return true;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            return true;
        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (buffType == BuffableValue.PercentDamageReceived)
            {
                buffedValue *= Config.Amount;
            }
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }
    }
}
