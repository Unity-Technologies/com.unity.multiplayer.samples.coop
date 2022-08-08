using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// A version of LaunchProjectileAction that can be "powered up" by holding down the attack key.
    /// </summary>
    /// <remarks>
    /// The player can hold down the button for this ability to "charge it up" and make it more effective. Once it's been
    /// charging for Description.ExecTimeSeconds, it reaches maximum charge. If the player is attacked by an enemy, that
    /// also immediately stops the charge-up, but also cancels firing.
    ///
    /// Once charge-up stops, the projectile is fired (unless it was stopped due to being attacked.)
    ///
    /// The projectile can have various stats depending on how "charged up" the attack was. The ActionDescription's
    /// Projectiles array should contain each tier of projectile, sorted from weakest to strongest.
    ///
    /// </remarks>

    [CreateAssetMenu()]
    public class ChargedLaunchProjectileAction : LaunchProjectileAction
    {
        /// <summary>
        /// Set once we've stopped charging up, for any reason:
        /// - the player has let go of the button,
        /// - we were attacked,
        /// - or the maximum charge was reached.
        /// </summary>
        private float m_StoppedChargingUpTime = 0;

        /// <summary>
        /// Were we attacked while charging up? (If so, we won't actually fire.)
        /// </summary>
        private bool m_HitByAttack = false;

        /// <summary>
        /// A list of the special particle graphics we spawned.
        /// </summary>
        /// <remarks>
        /// Performance note: repeatedly creating and destroying GameObjects is not optimal, and on low-resource platforms
        /// (like mobile devices), it can lead to major performance problems. On mobile platforms, visual graphics should
        /// use object-pooling (i.e. reusing the same GameObjects repeatedly). But that's outside the scope of this demo.
        /// </remarks>
        private List<SpecialFXGraphic> m_Graphics = new List<SpecialFXGraphic>();


        private bool m_ChargeEnded;

        public override bool OnStart(ServerCharacter parent)
        {
            // if we have an explicit target, make sure we're aimed at them.
            // (But if the player just clicked on an attack button, there won't be an explicit target, so we should stay facing however we're facing.)
            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkObject initialTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    // face our target
                    parent.physicsWrapper.Transform.LookAt(initialTarget.transform.position);
                }
            }

            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            // start the "charging up" ActionFX
            parent.NetState.RecvDoActionClientRPC(Data);

            // sanity-check our data a bit
            Debug.Assert(Config.Projectiles.Length > 1, $"Action {name} has {Config.Projectiles.Length} Projectiles. Expected at least 2!");
            foreach (var projectileInfo in Config.Projectiles)
            {
                Debug.Assert(projectileInfo.ProjectilePrefab, $"Action {name}: one of the Projectiles is missing its prefab!");
                Debug.Assert(projectileInfo.Range > 0, $"Action {name}: one of the Projectiles has invalid Range!");
                Debug.Assert(projectileInfo.Speed_m_s > 0, $"Action {name}: one of the Projectiles has invalid Speed_m_s!");
            }
            return true;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (m_StoppedChargingUpTime == 0 && GetPercentChargedUp() >= 1)
            {
                // we haven't explicitly stopped charging up... but we've reached max charge, so that implicitly stops us
                StopChargingUp(parent);
            }

            // we end as soon as we've stopped charging up (and have fired the projectile)
            return m_StoppedChargingUpTime == 0;
        }

        public override void OnGameplayActivity(ServerCharacter parent, GameplayActivity activityType)
        {
            if (activityType == GameplayActivity.AttackedByEnemy)
            {
                // if we get attacked while charging up, we don't actually get to shoot!
                m_HitByAttack = true;
                StopChargingUp(parent);
            }
            else if (activityType == GameplayActivity.StoppedChargingUp)
            {
                StopChargingUp(parent);
            }
        }

        public override void Cancel(ServerCharacter parent)
        {
            StopChargingUp(parent);
        }

        public override void End(ServerCharacter parent)
        {
            StopChargingUp(parent);
        }

        private void StopChargingUp(ServerCharacter parent)
        {
            if (m_StoppedChargingUpTime == 0)
            {
                m_StoppedChargingUpTime = Time.time;

                if (!string.IsNullOrEmpty(Config.Anim2))
                {
                    parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
                }

                parent.NetState.RecvStopChargingUpClientRpc(GetPercentChargedUp());
                if (!m_HitByAttack)
                {
                    LaunchProjectile(parent);
                }
            }
        }

        private float GetPercentChargedUp()
        {
            return ActionUtils.GetPercentChargedUp(m_StoppedChargingUpTime, TimeRunning, TimeStarted, Config.ExecTimeSeconds);
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
        protected override ActionConfig.ProjectileInfo GetProjectileInfo()
        {
            if (Config.Projectiles.Length == 0) // uh oh, this is bad data
                throw new System.Exception($"Action {name} has no Projectiles!");

            // choose which prefab to use based on how charged-up we got.
            // Note how we cast the result to an int, which implicitly rounds down.
            // Thus, only a 100% maxed charge can return the most powerful prefab.
            int projectileIdx = (int)(GetPercentChargedUp() * (Config.Projectiles.Length - 1));

            return Config.Projectiles[projectileIdx];
        }

        public override bool OnStartClient(ClientCharacterVisualization parent)
        {
            base.OnStartClient(parent);

            m_Graphics = InstantiateSpecialFXGraphics(parent.transform, true);
            return true;
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            return !m_ChargeEnded;
        }

        public override void CancelClient(ClientCharacterVisualization parent)
        {
            if (!m_ChargeEnded)
            {
                foreach (var graphic in m_Graphics)
                {
                    if (graphic)
                    {
                        graphic.Shutdown();
                    }
                }
            }
        }

        public override void OnStoppedChargingUpClient(ClientCharacterVisualization parent, float finalChargeUpPercentage)
        {
            m_ChargeEnded = true;
            foreach (var graphic in m_Graphics)
            {
                if (graphic)
                {
                    graphic.Shutdown();
                }
            }
            // the graphics will now take care of themselves and shutdown, so we can forget about 'em
            m_Graphics.Clear();
        }

    }
}
