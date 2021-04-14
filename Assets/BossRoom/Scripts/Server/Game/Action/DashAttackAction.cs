using MLAPI;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Server
{
    /// <summary>
    /// Causes the attacker to dash toward a target. When they stop dashing, they perform a melee attack.
    /// The character is immune to damage while dashing.
    ///
    /// Since the "Range" field means "range when we can begin dashing at our target", we need another
    /// field to mean "range of our melee attack after dashing". We'll use the "Radius" field of the
    /// ActionDescription for that.
    /// 
    /// </summary>
    /// <remarks>
    /// See MeleeAction for relevant discussion about targeting; we use the same concept here: preferring
    /// the chosen target, but using whatever is actually within striking distance at time of attack.
    /// </remarks>
    /// 
    public class DashAttackAction : Action
    {
        private bool m_StartedDash;
        private bool m_StoppedDash;
        private float m_DashDuration;

        public DashAttackAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            Assert.IsTrue(Description.MoveSpeed > 0, $"ActionDescription for {Description.ActionTypeEnum} needs a MoveSpeed assigned!");
            Assert.IsTrue(Description.Radius > 0, $"ActionDescription for {Description.ActionTypeEnum} needs a Radius assigned!");
        }

        public override bool Start()
        {
            // snap to face our destination. This is the direction we'll charge in
            m_Parent.transform.LookAt(GetTargetSpot());

            // tell clients to visualize this action
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            if (!m_StartedDash && TimeRunning >= Description.ExecTimeSeconds)
            {
                // time to start dashing!
                m_StartedDash = true;
                float distanceToTargetPos = Vector3.Distance(GetTargetSpot(), m_Parent.transform.position);
                m_DashDuration = distanceToTargetPos / Description.MoveSpeed;

                // actually start the movement
                var movement = m_Parent.GetComponent<ServerCharacterMovement>();
                movement.StartForwardCharge(Description.MoveSpeed, m_DashDuration);
            }

            if (m_StartedDash && !m_StoppedDash && TimeRunning >= Description.ExecTimeSeconds + m_DashDuration)
            {
                // time to stop dashing and strike!
                m_StoppedDash = true;
                PerformMeleeAttack();
            }

            return !m_StoppedDash;
        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (m_StartedDash && !m_StoppedDash && buffType == BuffableValue.PercentDamageReceived)
            {
                // we suffer no damage while dashing
                buffedValue = 0;
            }
        }

        /// <summary>
        /// Returns the targeted NetworkObject, or null if no valid target specified.
        /// </summary>
        private NetworkObject GetTarget()
        {
            if (Data.TargetIds != null && Data.TargetIds.Length > 0)
            {
                return NetworkSpawnManager.SpawnedObjects[Data.TargetIds[0]];
            }
            return null;
        }

        /// <summary>
        /// Returns the spot we're dashing to.
        /// </summary>
        private Vector3 GetTargetSpot()
        {
            var targetObject = GetTarget();
            if (targetObject)
            {
                return targetObject.transform.position;
            }
            else
            {
                return Data.Position;
            }
        }

        private void PerformMeleeAttack()
        {
            RaycastHit[] results;
            // perform a typical melee-hit. But note that we are using the Radius field for range, not the Range field!
            int numResults = ActionUtils.DetectMeleeFoe(Description.IsFriendly ^ m_Parent.IsNpc, m_Parent.GetComponent<Collider>(), Description.Radius, out results);
            if (numResults == 0) { return; }

            // everything that gets returned should have have a component that is IDamageable.
            IDamageable foundFoe = results[0].collider.GetComponent<IDamageable>();

            // We defaulted to the first foe in the array, but if we find the user-selected target, use that instead
            var provisionalTarget = GetTarget();
            if (provisionalTarget)
            {
                for (int i = 0; i < numResults; i++)
                {
                    var damageable = results[i].collider.GetComponent<IDamageable>();
                    if (damageable != null && damageable.NetworkObjectId == provisionalTarget.NetworkObjectId)
                    {
                        foundFoe = damageable;
                        break;
                    }
                }
            }

            if (foundFoe != null)
            {
                foundFoe.ReceiveHP(m_Parent, -Description.Amount);
            }
        }
    }
}
