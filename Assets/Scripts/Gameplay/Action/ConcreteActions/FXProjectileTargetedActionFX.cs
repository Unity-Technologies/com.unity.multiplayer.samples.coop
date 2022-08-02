using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Controls the visuals for an always-hit-projectile attack. See FXProjectileTargetedAction.cs for more about this action type.
    /// </summary>
    public class FXProjectileTargetedActionFX : ActionFX
    {
        public FXProjectileTargetedActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent) : base(ref data, clientParent) { }

        // have we actually played an impact?
        private bool m_ImpactPlayed;

        // the time the FX projectile spends in the air
        private float m_ProjectileDuration;

        // the currently-live projectile. (Note that the projectile will normally destroy itself! We only care in case someone calls Cancel() on us)
        private FXProjectile m_Projectile;

        // the enemy we're aiming at
        private NetworkObject m_Target;

        Transform m_TargetTransform;

        public override bool OnStartClient()
        {
            base.OnStartClient();
            m_Target = GetTarget();

            if (m_Target && PhysicsWrapper.TryGetPhysicsWrapper(m_Target.NetworkObjectId, out var physicsWrapper))
            {
                m_TargetTransform = physicsWrapper.Transform;
            }

            if (c_Description.Projectiles.Length < 1 || c_Description.Projectiles[0].ProjectilePrefab == null)
                throw new System.Exception($"Action {c_Description.ActionTypeEnum} has no valid ProjectileInfo!");

            return true;
        }

        public override bool OnUpdateClient()
        {
            if (c_TimeRunning >= c_Description.ExecTimeSeconds && m_Projectile == null)
            {
                // figure out how long the pretend-projectile will be flying to the target
                var targetPos = m_TargetTransform ? m_TargetTransform.position : m_CData.Position;
                var initialDistance = Vector3.Distance(targetPos, m_ClientParent.transform.position);
                m_ProjectileDuration = initialDistance / c_Description.Projectiles[0].Speed_m_s;

                // create the projectile. It will control itself from here on out
                m_Projectile = SpawnAndInitializeProjectile();
            }

            // we keep going until the projectile's duration ends
            return c_TimeRunning <= m_ProjectileDuration + c_Description.ExecTimeSeconds;
        }

        public override void CancelClient()
        {
            if (m_Projectile)
            {
                // we aborted post-projectile-launch (somehow)! Tell the graphics! (It will destroy itself, possibly after playing some more FX)
                m_Projectile.Cancel();
            }
        }

        public override void EndClient()
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
                var hitReact = !string.IsNullOrEmpty(c_Description.ReactAnim) ? c_Description.ReactAnim : k_DefaultHitReact;
                clientCharacter.ChildVizObject.OurAnimator.SetTrigger(hitReact);
            }
        }

        private NetworkObject GetTarget()
        {
            if (c_Data.TargetIds == null || c_Data.TargetIds.Length == 0)
            {
                return null;
            }

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(c_Data.TargetIds[0], out NetworkObject targetObject) && targetObject != null)
            {
                // make sure this isn't a friend (or if it is, make sure this is a friendly-fire action)
                var targetable = targetObject.GetComponent<ITargetable>();
                if (targetable != null && targetable.IsNpc == (c_Description.IsFriendly ^ IsParentAnNPC()))
                {
                    // not a valid target
                    return null;
                }
                return targetObject;
            }
            else
            {
                // target could have legitimately disappeared in the time it took to queue this action... but that's pretty unlikely, so we'll log about it to ease debugging
                Debug.Log($"FXProjectileTargetedActionFX was targeted at ID {c_Data.TargetIds[0]}, but that target can't be found in spawned object list! (May have just been deleted?)");
                return null;
            }
        }

        /// <summary>
        /// Determines if the character playing this Action is an NPC (as opposed to a player)
        /// </summary>
        private bool IsParentAnNPC()
        {
            return m_ClientParent.NetState.IsNpc;
        }


        private FXProjectile SpawnAndInitializeProjectile()
        {
            var projectileGO = Object.Instantiate(c_Description.Projectiles[0].ProjectilePrefab, m_ClientParent.transform.position, m_ClientParent.transform.rotation, null);

            var projectile = projectileGO.GetComponent<FXProjectile>();
            if (!projectile)
            {
                throw new System.Exception($"FXProjectileTargetedAction tried to spawn projectile {projectileGO.name}, as dictated for action type {c_Data.ActionTypeEnum}, but the object doesn't have a FXProjectile component!");
            }

            // now that we have our projectile, initialize it so it'll fly at the target appropriately
            projectile.Initialize(m_ClientParent.transform.position, m_TargetTransform, m_CData.Position, m_ProjectileDuration);
            return projectile;
        }

        public override void AnticipateActionClient()
        {
            base.AnticipateActionClient();

            // see if this is going to be a "miss" because the player tried to click through a wall. If so,
            // we change our data in the same way that the server will (changing our target point to the spot on the wall)
            Vector3 targetSpot = c_Data.Position;
            if (c_Data.TargetIds != null && c_Data.TargetIds.Length > 0)
            {
                var targetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[c_Data.TargetIds[0]];
                if (targetObj)
                {
                    targetSpot = targetObj.transform.position;
                }
            }

            if (!ActionUtils.HasLineOfSight(m_ClientParent.transform.position, targetSpot, out Vector3 collidePos))
            {
                // we do not have line of sight to the target point. So our target instead becomes the obstruction point
                c_Data.TargetIds = null;
                c_Data.Position = collidePos;
            }
        }
    }
}
