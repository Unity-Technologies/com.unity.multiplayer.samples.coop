using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Controls the visuals for an always-hit-projectile attack. See FXProjectileTargetedAction.cs for more about this action type.
    /// </summary>
    public class FXProjectileTargetedActionFX : ActionFX
    {
        public FXProjectileTargetedActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        // have we actually played an impact?
        private bool m_ImpactPlayed;

        // the time the FX projectile spends in the air
        private float m_ProjectileDuration;

        // the currently-live projectile. (Note that the projectile will normally destroy itself! We only care in case someone calls Cancel() on us)
        private FXProjectile m_Projectile;

        // the enemy we're aiming at
        private Client.ClientCharacter m_Target;

        public override bool Start()
        {
            m_Target = GetTarget();
            if (HasTarget() && m_Target == null)
            {
                // target has disappeared! Abort.
                return false;
            }

            // figure out how long the pretend-projectile will be flying to the target
            Vector3 targetPos = HasTarget() ? m_Target.transform.position : m_Data.Position;
            float initialDistance = Vector3.Distance(targetPos, m_Parent.transform.position);
            m_ProjectileDuration = initialDistance / Description.ProjectileSpeed_m_s;

            // create the projectile. It will control itself from here on out
            m_Projectile = SpawnAndInitializeProjectile();

            // animate shooting the projectile
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
            return true;
        }

        public override bool Update()
        {
            // we keep going until the projectile's duration ends
            return (Time.time - TimeStarted) <= m_ProjectileDuration + Description.ExecTimeSeconds;
        }

        public override void OnAnimEvent(string id)
        {
            //Debug.Log($"Anim event: {id}");
        }

        public override void Cancel()
        {
            if (m_Projectile)
            {
                // we aborted post-projectile-launch (somehow)! Tell the graphics! (It will destroy itself, possibly after playing some more FX)
                m_Projectile.Cancel();
            }
        }

        public override void End()
        {
            PlayHitReact();
        }

        private void PlayHitReact()
        {
            if (m_ImpactPlayed)
                return;
            m_ImpactPlayed = true;

            if (m_Target)
            {
                ClientCharacterVisualization targetViz = m_Target.GetComponent<Client.ClientCharacter>().ChildVizObject;
                targetViz.OurAnimator.SetTrigger("HitReact1");
            }
        }

        /// <summary>
        /// Do we even have a target? (If false, it means player clicked on nothing, and we're rendering a "missed" fake bolt.)
        /// </summary>
        private bool HasTarget()
        {
            return Data.TargetIds != null && Data.TargetIds.Length > 0;
        }

        private Client.ClientCharacter GetTarget()
        {
            if (Data.TargetIds == null || Data.TargetIds.Length == 0)
            {
                return null;
            }

            NetworkedObject obj;
            if (SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out obj) && obj != null)
            {
                return obj.GetComponent<Client.ClientCharacter>();
            }
            else
            {
                // target could have legitimately disappeared in the time it took to queue this action... but that's pretty unlikely, so we'll log about it to ease debugging
                Debug.Log($"FXProjectileTargetedActionFX was targeted at ID {Data.TargetIds[0]}, but that target can't be found in spawned object list! (May have just been deleted?)");
                return null;
            }
        }

        private FXProjectile SpawnAndInitializeProjectile()
        {
            GameObject projectileGO = Object.Instantiate(Description.Spawns[0], m_Parent.transform.position, m_Parent.transform.rotation, null);

            FXProjectile projectile = projectileGO.GetComponent<FXProjectile>();
            if (!projectile)
            {
                throw new System.Exception($"FXProjectileTargetedAction tried to spawn projectile {projectileGO.name}, as dictated for action type {Data.ActionTypeEnum}, but the object doesn't have a FXProjectile component!");
            }

            // now that we have our projectile, initialize it so it'll fly at the target appropriately
            projectile.Initialize(m_Parent.transform.position, m_Target?.transform, m_Data.Position, Description.ExecTimeSeconds, m_ProjectileDuration);
            return projectile;
        }
    }
}
