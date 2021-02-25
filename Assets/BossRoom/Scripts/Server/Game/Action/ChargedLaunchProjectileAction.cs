using MLAPI;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// A version of LaunchProjectileAction that can be "powered up" by holding down the attack key.
    /// </summary>
    /// <remarks>
    /// The player can hold down the button for this ability to "charge it up" and make it more effective. Once it's been
    /// charging for Description.ExecTimeSeconds, it reaches maximum charge. If the player is attacked by an enemy, that
    /// also immediately stops the charge-up. Once charge-up stops, the projectile is fired.
    ///
    /// The projectile can have various statistics depending on how "charged up" the attack was. The ActionDescription's
    /// Projectiles array should contain each tier of projectile, sorted from weakest to strongest.
    /// 
    /// </remarks>
    public class ChargedLaunchProjectileAction : LaunchProjectileAction
    {
        /// <summary>
        /// Set once we've stopped charging up, for any reason:
        /// - the player has let go of the button,
        /// - we were attacked,
        /// - or the maximum charge was reached.
        /// </summary>
        private float m_StoppedChargingUpTime = 0;

        public ChargedLaunchProjectileAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            // if we have an explicit target, make sure we're aimed at them.
            // (But if the player just clicked on an attack button, there won't be an explicit target, so we should stay facing however we're facing.)
            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkedObject initialTarget = MLAPI.Spawning.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    // face our target
                    m_Parent.transform.LookAt(initialTarget.transform.position);
                }
            }

            // start the "charging up" ActionFX
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            if (m_StoppedChargingUpTime == 0)
            {
                // we haven't explicitly stopped charging up... but if we've reached max charge, that implicitly stops us
                if (TimeRunning >= Description.ExecTimeSeconds)
                {
                    StopChargingUp();
                }
            }

            // we end as soon as we've stopped charging up (and have fired the projectile)
            return m_StoppedChargingUpTime == 0;
        }

        public override void OnGameplayActivity(GameplayActivity activityType)
        {
            // for this particular type of Action, being attacked immediately causes you to stop charging up
            if (activityType == GameplayActivity.AttackedByEnemy || activityType == GameplayActivity.StoppedChargingUp)
            {
                StopChargingUp();
            }
        }

        public override void Cancel()
        {
            StopChargingUp();
        }

        public override void End()
        {
            StopChargingUp();
        }

        private void StopChargingUp()
        {
            if (m_StoppedChargingUpTime == 0)
            {
                m_StoppedChargingUpTime = Time.time;
                m_Parent.NetState.RecvStopChargingUpClientRpc();
                LaunchProjectile();
            }
        }

        /// <summary>
        /// Overridden from base-class to choose a different projectile depending on how "charged up" we got.
        /// To do this, we assume that the Projectiles list is ordered from weakest to strongest.
        /// </summary>
        /// <remarks>
        /// To reward players that fully charge-up their attack, we only return the strongest projectile when the
        /// charge-up is at 100%. The other tiers of projectile are used for lesser charge-up amounts.
        /// </remarks>
        /// <returns>the projectile that should be used</returns>
        protected override ActionDescription.ProjectileInfo GetProjectileInfo()
        {
            var possibilities = new List<ActionDescription.ProjectileInfo>();
            foreach (var projectileInfo in Description.Projectiles)
            {
                if (projectileInfo.ProjectilePrefab == null)
                    throw new System.Exception($"Action {Description.ActionTypeEnum}: one of the Projectiles is missing its prefab!");
                possibilities.Add(projectileInfo);
            }

            if (possibilities.Count == 0) // uh oh, this is bad data
                throw new System.Exception($"Action {Description.ActionTypeEnum} has no Projectiles!");

            if (possibilities.Count == 1)
            {
                // this is technically not invalid data, but is almost certainly not what was intended... so warn about it
                Debug.LogWarning($"Action {Data.ActionTypeEnum} has only 1 projectile prefab. We'll use the same prefab no matter how charged-up the shot is! Weird and probably wrong!");
                return possibilities[0];
            }

            float timeSpentChargingUp = m_StoppedChargingUpTime - TimeStarted;
            float pctChargedUp = Mathf.Clamp01(timeSpentChargingUp / Description.ExecTimeSeconds);

            // Finally, choose which prefab to use based on how charged-up we got.
            // Note how we cast the result to an int, which implicitly rounds down.
            // Thus, only a 100% maxed charge can return the most powerful prefab.
            int projectileIdx = (int)(pctChargedUp * (possibilities.Count - 1));

            return possibilities[projectileIdx];
        }

    }
}
