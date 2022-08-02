using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Causes the attacker to teleport near a target spot, then perform a melee attack. The client
    /// visualization moves the character locally beforehand, making the character appear to dash to the
    /// destination spot.
    ///
    /// After the ExecTime has elapsed, the character is immune to damage until the action ends.
    ///
    /// Since the "Range" field means "range when we can teleport to our target", we need another
    /// field to mean "range of our melee attack after dashing". We'll use the "Radius" field of the
    /// ActionDescription for that.
    /// </summary>
    /// <remarks>
    /// See MeleeAction for relevant discussion about targeting; we use the same concept here: preferring
    /// the chosen target, but using whatever is actually within striking distance at time of attack.
    /// </remarks>
    public class DashAttackAction : Action
    {
        private Vector3 m_TargetSpot;

        public DashAttackAction( ref ActionRequestData data) : base(ref data)
        {
            Assert.IsTrue(Description.Radius > 0, $"ActionDescription for {Description.ActionTypeEnum} needs a Radius assigned!");
        }

        public override bool OnStart(ServerCharacter parent)
        {
            // remember the exact spot we'll stop.
            m_TargetSpot = ActionUtils.GetDashDestination(parent.physicsWrapper.Transform, Data.Position, true, Description.Range, Description.Range);

            // snap to face our destination. This ensures the client visualization faces the right way while "pretending" to dash
            parent.physicsWrapper.Transform.LookAt(m_TargetSpot);

            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);

            // tell clients to visualize this action
            parent.NetState.RecvDoActionClientRPC(Data);

            return ActionConclusion.Continue;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            return ActionConclusion.Continue;
        }

        public override void End(ServerCharacter parent)
        {
            // Anim2 contains the name of the end-loop-sequence trigger
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }

            // we're done, time to teleport!
            parent.Movement.Teleport(m_TargetSpot);

            // and then swing!
            PerformMeleeAttack();
        }

        public override void Cancel(ServerCharacter parent)
        {
            // OtherAnimatorVariable contains the name of the cancellation trigger
            if (!string.IsNullOrEmpty(Description.OtherAnimatorVariable))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.OtherAnimatorVariable);
            }

            // because the client-side visualization of the action moves the character visualization around,
            // we need to explicitly end the client-side visuals when we abort
            parent.NetState.RecvCancelActionsByTypeClientRpc(Description.ActionTypeEnum);

        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (TimeRunning >= Description.ExecTimeSeconds && buffType == BuffableValue.PercentDamageReceived)
            {
                // we suffer no damage during the "dash" (client-side pretend movement)
                buffedValue = 0;
            }
        }

        private void PerformMeleeAttack(ServerCharacter parent)
        {
            // perform a typical melee-hit. But note that we are using the Radius field for range, not the Range field!
            IDamageable foe = MeleeAction.GetIdealMeleeFoe(Description.IsFriendly ^ parent.IsNpc,
                parent.physicsWrapper.DamageCollider,
                                                            Description.Radius,
                                                            (Data.TargetIds != null && Data.TargetIds.Length > 0 ? Data.TargetIds[0] : 0));

            if (foe != null)
            {
                foe.ReceiveHP(parent, -Description.Amount);
            }
        }
    }
}
