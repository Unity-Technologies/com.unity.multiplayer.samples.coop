using System;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Action that represents an always-hit raybeam-style ranged attack. A particle is shown from caster to target, and then the
    /// target takes damage. (It is not possible to escape the hit; the target ALWAYS takes damage.) This is intended for fairly
    /// swift particles; the time before it's applied is based on a simple distance-check at the attack's start.
    /// (If no target is provided (because the user clicked on an empty spot on the map) or if the caster doesn't have line of
    /// sight to the target (because it's behind a wall), we still perform an action, it just hits nothing.
    /// </summary>

    [CreateAssetMenu(menuName = "BossRoom/Actions/FX Projectile Targeted Action")]
    public partial class FXProjectileTargetedAction : Action
    {
        private bool m_ImpactedTarget;
        private float m_TimeUntilImpact;
        private IDamageable m_DamageableTarget;

        public override bool OnStart(ServerCharacter parent)
        {
            m_DamageableTarget = GetDamageableTarget(parent);

            // figure out where the player wants us to aim at...
            Vector3 targetPos = m_DamageableTarget != null ? m_DamageableTarget.transform.position : m_Data.Position;

            // then make sure we can actually see that point!
            if (!ActionUtils.HasLineOfSight(parent.physicsWrapper.Transform.position, targetPos, out Vector3 collidePos))
            {
                // we do not have line of sight to the target point. So our target instead becomes the obstruction point
                m_DamageableTarget = null;
                targetPos = collidePos;

                // and update our action data so that when we send it to the clients, it will be up-to-date
                Data.TargetIds = new ulong[0];
                Data.Position = targetPos;
            }

            // turn to face our target!
            parent.physicsWrapper.Transform.LookAt(targetPos);

            // figure out how long the pretend-projectile will be flying to the target
            float distanceToTargetPos = Vector3.Distance(targetPos, parent.physicsWrapper.Transform.position);
            m_TimeUntilImpact = Config.ExecTimeSeconds + (distanceToTargetPos / Config.Projectiles[0].Speed_m_s);

            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
            // tell clients to visualize this action
            parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override void Reset()
        {
            base.Reset();

            m_ImpactedTarget = false;
            m_TimeUntilImpact = 0;
            m_DamageableTarget = null;
            m_ImpactPlayed = false;
            m_ProjectileDuration = 0;
            m_Projectile = null;
            m_Target = null;
            m_TargetTransform = null;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (!m_ImpactedTarget && m_TimeUntilImpact <= TimeRunning)
            {
                m_ImpactedTarget = true;
                if (m_DamageableTarget != null)
                {
                    m_DamageableTarget.ReceiveHP(parent, -Config.Projectiles[0].Damage);
                }
            }
            return true;
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!m_ImpactedTarget)
            {
                parent.NetState.RecvCancelActionsByPrototypeIDClientRpc(ActionID);
            }
        }

        /// <summary>
        /// Returns our intended target, or null if not found/no target.
        /// </summary>
        private IDamageable GetDamageableTarget(ServerCharacter parent)
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
                if (serverChar && serverChar.IsNpc == (Config.IsFriendly ^ parent.IsNpc))
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
