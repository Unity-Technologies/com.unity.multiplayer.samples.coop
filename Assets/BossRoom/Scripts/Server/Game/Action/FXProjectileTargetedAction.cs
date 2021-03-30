using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Action that represents an always-hit raybeam-style ranged attack. A particle is shown from caster to target, and then the
    /// target takes damage. (It is not possible to escape the hit; the target ALWAYS takes damage.) This is intended for fairly
    /// swift particles; the time before it's applied is based on a simple distance-check at the attack's start.
    /// (If no target is provided, it means the user clicked on an empty spot on the map. In that case we still perform an action,
    /// it just hits nothing.)
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
            if (m_Target == null && HasTarget())
            {
                // target has disappeared! Abort.
                return false;
            }

            Vector3 targetPos = HasTarget() ? m_Target.transform.position : m_Data.Position;

            // turn to face our target!
            m_Parent.transform.LookAt(targetPos);

            // figure out how long the pretend-projectile will be flying to the target
            float distanceToTargetPos = Vector3.Distance(targetPos, m_Parent.transform.position);
            m_TimeUntilImpact = Description.ExecTimeSeconds + (distanceToTargetPos / Description.Projectiles[0].Speed_m_s);
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            if (!m_ImpactedTarget && m_TimeUntilImpact <= (Time.time - TimeStarted))
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
            // TODO: somehow tell the corresponding FX to abort!
        }

        /// <summary>
        /// Are we even supposed to have a target? (If not, we're representing a "missed" bolt that just hits nothing.)
        /// </summary>
        private bool HasTarget()
        {
            return Data.TargetIds != null && Data.TargetIds.Length > 0;
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
            if (NetworkSpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out obj) && obj != null)
            {
                return obj.GetComponent<IDamageable>();
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
