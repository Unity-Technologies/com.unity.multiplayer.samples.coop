using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Action that represents an always-hit raybeam-style ranged attack. A particle is shown from caster to target, and then the
    /// target takes damage. (It is not possible to escape the hit; the target ALWAYS takes damage.) This is intended for fairly
    /// swift particles; the time before it's applied is based on a simple distance-check at the attack's start.
    /// (If no target is provided (because the user clicked on an empty spot on the map) or if the caster doesn't have line of
    /// sight to the target (because it's behind a wall), we still perform an action, it just hits nothing.
    /// </summary>
    public class FXProjectileTargetedAction : Action
    {
        private bool m_ImpactedTarget;
        private float m_TimeUntilImpact;
        private IDamageable m_Target;

        public FXProjectileTargetedAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            m_Target = GetTarget();

            // figure out where the player wants us to aim at...
            Vector3 targetPos = m_Target != null ? m_Target.transform.position : m_Data.Position;

            // then make sure we can actually see that point!
            if (!ActionUtils.HasLineOfSight(m_Parent.physicsWrapper.Transform.position, targetPos, out Vector3 collidePos))
            {
                // we do not have line of sight to the target point. So our target instead becomes the obstruction point
                m_Target = null;
                targetPos = collidePos;

                // and update our action data so that when we send it to the clients, it will be up-to-date
                Data.TargetIds = new ulong[0];
                Data.Position = targetPos;
            }

            // turn to face our target!
            m_Parent.physicsWrapper.Transform.LookAt(targetPos);

            // figure out how long the pretend-projectile will be flying to the target
            float distanceToTargetPos = Vector3.Distance(targetPos, m_Parent.physicsWrapper.Transform.position);
            m_TimeUntilImpact = Description.ExecTimeSeconds + (distanceToTargetPos / Description.Projectiles[0].Speed_m_s);

            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            // tell clients to visualize this action
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            if (!m_ImpactedTarget && m_TimeUntilImpact <= TimeRunning)
            {
                m_ImpactedTarget = true;
                if (m_Target != null )
                {
                    m_Target.ReceiveHP(m_Parent, -Description.Projectiles[0].Damage);
                }
            }
            return true;
        }

        public override void Cancel()
        {
            if (!m_ImpactedTarget)
            {
                m_Parent.NetState.RecvCancelActionsByTypeClientRpc(Description.ActionTypeEnum);
            }
        }

        /// <summary>
        /// Returns our intended target, or null if not found/no target.
        /// </summary>
        private IDamageable GetTarget()
        {
            if (Data.TargetIds == null || Data.TargetIds.Length == 0)
            {
                return null;
            }

            NetworkObject obj;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out obj) && obj != null)
            {
                // make sure this isn't a friend (or if it is, make sure this is a friendly-fire action)
                var serverChar = obj.GetComponent<ServerCharacter>();
                if (serverChar && serverChar.IsNpc == (Description.IsFriendly ^ m_Parent.IsNpc))
                {
                    // not a valid target
                    return null;
                }

                if (PhysicsWrapper.TryGetPhysicsWrapper(Data.TargetIds[0], out var physicsWrapper))
                {
                    return physicsWrapper.DamageCollider.GetComponent<IDamageable>();
                }
                else
                {
                    return obj.GetComponent<IDamageable>();
                }
            }
            else
            {
                // target could have legitimately disappeared in the time it took to queue this action... but that's pretty unlikely, so we'll log about it to ease debugging
                Debug.Log($"FXProjectileTargetedAction was targeted at ID {Data.TargetIds[0]}, but that target can't be found in spawned object list! (May have just been deleted?)");
                return null;
            }
        }
    }
}
