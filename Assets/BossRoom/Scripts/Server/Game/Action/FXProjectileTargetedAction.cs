using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Action that represents an always-hit raybeam-style ranged attack. A particle is shown from caster to target, and then the
    /// target takes damage. (It is not possible to escape the hit; the target ALWAYS takes damage.) This is intended for fairly
    /// swift particles; the time before it's applied is based on a simple distance-check at the attack's start.
    /// </summary>
    public class FXProjectileTargetedAction : Action
    {
        private bool m_ImpactedTarget;
        private float m_TimeUntilImpact;
        private ServerCharacter m_Target;

        public FXProjectileTargetedAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            if (data.TargetIds == null || data.TargetIds.Length == 0)
                throw new System.Exception("FXProjectileTargetedAction requires a target; no target provided!");
        }

        public override bool Start()
        {
            m_Target = GetTarget();
            if (m_Target == null)
            {
                // target has disappeared! Abort.
                return false;
            }

            float distanceToTarget = Vector3.Distance(m_Target.transform.position, m_Parent.transform.position);
            m_TimeUntilImpact = Description.ExecTimeSeconds + ActionUtils.CalculateFXProjectileDuration(Description, distanceToTarget);
            m_Parent.NetState.ServerBroadcastAction(ref Data);
            return true;
        }

        public override bool Update()
        {
            if (!m_ImpactedTarget && m_TimeUntilImpact <= (Time.time - TimeStarted))
            {
                m_ImpactedTarget = true;
                if (m_Target)
                {
                    m_Target.ReceiveHP(this.m_Parent, -Description.Amount);
                }
            }
            return true;
        }

        public override void Cancel()
        {
            // TODO: somehow tell the corresponding FX to abort!
        }

        private ServerCharacter GetTarget()
        {
            if (Data.TargetIds == null || Data.TargetIds.Length == 0)
            {
                throw new System.Exception("FXProjectileTargetedAction has no Targets! Cannot possibly fire a projectile!");
            }

            NetworkedObject obj;
            if (SpawnManager.SpawnedObjects.TryGetValue(Data.TargetIds[0], out obj) && obj != null)
            {
                return obj.GetComponent<ServerCharacter>();
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
