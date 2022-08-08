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
    [CreateAssetMenu()]
    public class FXProjectileTargetedAction : Action
    {
        private bool m_ImpactedTarget;
        private float m_TimeUntilImpact;
        private IDamageable m_DamageableTarget;

        // have we actually played an impact?
        private bool m_ImpactPlayed;

        // the time the FX projectile spends in the air
        private float m_ProjectileDuration;

        // the currently-live projectile. (Note that the projectile will normally destroy itself! We only care in case someone calls Cancel() on us)
        private FXProjectile m_Projectile;

        // the enemy we're aiming at
        private NetworkObject m_Target;

        Transform m_TargetTransform;

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
                parent.NetState.RecvCancelActionsByPrototypeIDClientRpc(PrototypeActionID);
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

        public override bool OnStartClient(ClientCharacterVisualization parent)
        {
            base.OnStartClient(parent);
            m_Target = GetTarget(parent);

            if (m_Target && PhysicsWrapper.TryGetPhysicsWrapper(m_Target.NetworkObjectId, out var physicsWrapper))
            {
                m_TargetTransform = physicsWrapper.Transform;
            }

            if (Config.Projectiles.Length < 1 || Config.Projectiles[0].ProjectilePrefab == null)
                throw new System.Exception($"Action {name} has no valid ProjectileInfo!");

            return true;
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && m_Projectile == null)
            {
                // figure out how long the pretend-projectile will be flying to the target
                var targetPos = m_TargetTransform ? m_TargetTransform.position : Data.Position;
                var initialDistance = Vector3.Distance(targetPos, parent.transform.position);
                m_ProjectileDuration = initialDistance / Config.Projectiles[0].Speed_m_s;

                // create the projectile. It will control itself from here on out
                m_Projectile = SpawnAndInitializeProjectile(parent);
            }

            // we keep going until the projectile's duration ends
            return TimeRunning <= m_ProjectileDuration + Config.ExecTimeSeconds;
        }

        public override void CancelClient(ClientCharacterVisualization parent)
        {
            if (m_Projectile)
            {
                // we aborted post-projectile-launch (somehow)! Tell the graphics! (It will destroy itself, possibly after playing some more FX)
                m_Projectile.Cancel();
            }
        }

        public override void EndClient(ClientCharacterVisualization parent)
        {
            PlayHitReact();
        }

        private void PlayHitReact()
        {
            if (m_ImpactPlayed)
                return;
            m_ImpactPlayed = true;

            if (NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (m_Target && m_Target.TryGetComponent(out Client.ClientCharacter clientCharacter) && clientCharacter.ChildVizObject != null)
            {
                var hitReact = !string.IsNullOrEmpty(Config.ReactAnim) ? Config.ReactAnim : k_DefaultHitReact;
                clientCharacter.ChildVizObject.OurAnimator.SetTrigger(hitReact);
            }
        }

        private NetworkObject GetTarget(ClientCharacterVisualization parent)
        {
            if (Data.TargetIds == null || Data.TargetIds.Length == 0)
            {
                return null;
            }

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out NetworkObject targetObject) && targetObject != null)
            {
                // make sure this isn't a friend (or if it is, make sure this is a friendly-fire action)
                var targetable = targetObject.GetComponent<ITargetable>();
                if (targetable != null && targetable.IsNpc == (Config.IsFriendly ^ parent.NetState.IsNpc))
                {
                    // not a valid target
                    return null;
                }
                return targetObject;
            }
            else
            {
                // target could have legitimately disappeared in the time it took to queue this action... but that's pretty unlikely, so we'll log about it to ease debugging
                Debug.Log($"FXProjectileTargetedActionFX was targeted at ID {Data.TargetIds[0]}, but that target can't be found in spawned object list! (May have just been deleted?)");
                return null;
            }
        }

        private FXProjectile SpawnAndInitializeProjectile(ClientCharacterVisualization parent)
        {
            var projectileGO = Object.Instantiate(Config.Projectiles[0].ProjectilePrefab, parent.transform.position, parent.transform.rotation, null);

            var projectile = projectileGO.GetComponent<FXProjectile>();
            if (!projectile)
            {
                throw new System.Exception($"FXProjectileTargetedAction tried to spawn projectile {projectileGO.name}, as dictated for action {name}, but the object doesn't have a FXProjectile component!");
            }

            // now that we have our projectile, initialize it so it'll fly at the target appropriately
            projectile.Initialize(parent.transform.position, m_TargetTransform, Data.Position, m_ProjectileDuration);
            return projectile;
        }

        public override void AnticipateActionClient(ClientCharacterVisualization parent)
        {
            base.AnticipateActionClient(parent);

            // see if this is going to be a "miss" because the player tried to click through a wall. If so,
            // we change our data in the same way that the server will (changing our target point to the spot on the wall)
            Vector3 targetSpot = Data.Position;
            if (Data.TargetIds != null && Data.TargetIds.Length > 0)
            {
                var targetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[Data.TargetIds[0]];
                if (targetObj)
                {
                    targetSpot = targetObj.transform.position;
                }
            }

            if (!ActionUtils.HasLineOfSight(parent.transform.position, targetSpot, out Vector3 collidePos))
            {
                // we do not have line of sight to the target point. So our target instead becomes the obstruction point
                Data.TargetIds = null;
                Data.Position = collidePos;
            }
        }
    }
}
