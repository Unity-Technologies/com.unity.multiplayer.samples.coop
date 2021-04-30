using MLAPI;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Server
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

        public DashAttackAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            Assert.IsTrue(Description.Radius > 0, $"ActionDescription for {Description.ActionTypeEnum} needs a Radius assigned!");
        }

        public override bool Start()
        {
            // remember the exact spot we'll stop.
            m_TargetSpot = ActionUtils.GetTeleportDestination(m_Parent.transform, Data.Position, true, Description.Range, Description.Range);

            // snap to face our destination. This ensures the client visualization faces the right way while "pretending" to dash
            m_Parent.transform.LookAt(m_TargetSpot);

            // tell clients to visualize this action
            m_Parent.NetState.RecvDoActionClientRPC(Data);

            return ActionConclusion.Continue;
        }

        public override bool Update()
        {
            return ActionConclusion.Continue;
        }

        public override void End()
        {
            // we're done, time to teleport!
            m_Parent.GetComponent<ServerCharacterMovement>().Teleport(m_TargetSpot);

            // and then swing!
            PerformMeleeAttack();
        }

        public override void Cancel()
        {
            // because the client-side visualization of the action moves the character visualization around,
            // we need to explicitly end the client-side visuals when we abort
            m_Parent.NetState.RecvCancelActionsByTypeClientRpc(Description.ActionTypeEnum);

        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (TimeRunning >= Description.ExecTimeSeconds && buffType == BuffableValue.PercentDamageReceived)
            {
                // we suffer no damage during the "dash" (client-side pretend movement)
                buffedValue = 0;
            }
        }

        private void PerformMeleeAttack()
        {
            // perform a typical melee-hit. But note that we are using the Radius field for range, not the Range field!
            IDamageable foe = MeleeAction.GetIdealMeleeFoe(Description.IsFriendly ^ m_Parent.IsNpc,
                                                            m_Parent.GetComponent<Collider>(),
                                                            Description.Radius,
                                                            (Data.TargetIds != null && Data.TargetIds.Length > 0 ? Data.TargetIds[0] : 0));

            if (foe != null)
            {
                foe.ReceiveHP(m_Parent, -Description.Amount);
            }
        }
    }
}
