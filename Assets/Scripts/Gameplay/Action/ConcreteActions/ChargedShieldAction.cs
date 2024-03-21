using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// A defensive action where the character becomes resistant to damage.
    /// </summary>
    /// <remarks>
    /// The player can hold down the button for this ability to "charge it up" and make it more effective. Once it's been
    /// charging for Description.ExecTimeSeconds, it reaches maximum charge. If the player is attacked by an enemy, that
    /// also immediately stops the charge-up.
    ///
    /// Once the charge-up stops (for any reason), the Action lasts for Description.EffectTimeSeconds before elapsing. During
    /// this time, all incoming damage is reduced by a percentage from 50% to 100%, depending on how "charged up" it was.
    ///
    /// When the Action is fully charged up, it provides a special additional benefit: if the boss tries to trample this
    /// character, the boss becomes Stunned.
    /// </remarks>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Charged Shield Action")]
    public partial class ChargedShieldAction : Action
    {
        /// <summary>
        /// Set once we've stopped charging up, for any reason:
        /// - the player has let go of the button,
        /// - we were attacked,
        /// - or the maximum charge was reached.
        /// </summary>
        private float m_StoppedChargingUpTime = 0;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkObject initialTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    // face our target, if we had one
                    serverCharacter.physicsWrapper.Transform.LookAt(initialTarget.transform.position);
                }
            }

            // because this action can be visually started and stopped as often and as quickly as the player wants, it's possible
            // for several copies of this action to be playing at once. This can lead to situations where several
            // dying versions of the action raise the end-trigger, but the animator only lowers it once, leaving the trigger
            // in a raised state. So we'll make sure that our end-trigger isn't raised yet. (Generally a good idea anyway.)
            serverCharacter.serverAnimationHandler.NetworkAnimator.ResetTrigger(Config.Anim2);

            // raise the start trigger to start the animation loop!
            serverCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            serverCharacter.clientCharacter.ClientPlayActionRpc(Data);
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            m_ChargeGraphics = null;
            m_ShieldGraphics = null;
            m_StoppedChargingUpTime = 0;
        }

        private bool IsChargingUp()
        {
            return m_StoppedChargingUpTime == 0;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (m_StoppedChargingUpTime == 0)
            {
                // we haven't explicitly stopped charging up... but if we've reached max charge, that implicitly stops us
                if (TimeRunning >= Config.ExecTimeSeconds)
                {
                    StopChargingUp(clientCharacter);
                }
            }

            // we stop once the charge-up has ended and our effect duration has elapsed
            return m_StoppedChargingUpTime == 0 || Time.time < (m_StoppedChargingUpTime + Config.EffectDurationSeconds);
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return m_StoppedChargingUpTime != 0;
        }

        private float GetPercentChargedUp()
        {
            return ActionUtils.GetPercentChargedUp(m_StoppedChargingUpTime, TimeRunning, TimeStarted, Config.ExecTimeSeconds);
        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (buffType == BuffableValue.PercentDamageReceived)
            {
                float percentChargedUp = GetPercentChargedUp();

                // the amount of damage reduction starts at 50% (for not-charged-up), then slowly increases to 100% depending on how charged-up we got
                float percentDamageReduction = 0.5f + ((percentChargedUp * percentChargedUp) / 2);

                // Now that we know how much damage to reduce it by, we need to set buffedValue to the inverse (because
                // it's looking for how much damage to DO, not how much to REDUCE BY). Also note how we don't just SET
                // buffedValue... we multiply our buff in with the current value. This lets our Action "stack"
                // with any other Actions that also alter this variable.)
                buffedValue *= 1 - percentDamageReduction;
            }
            else if (buffType == BuffableValue.ChanceToStunTramplers)
            {
                // if we are at "full charge", we stun enemies that try to trample us!
                if (GetPercentChargedUp() >= 1)
                {
                    buffedValue = 1;
                }
            }
        }

        public override void OnGameplayActivity(ServerCharacter serverCharacter, GameplayActivity activityType)
        {
            // for this particular type of Action, being attacked immediately causes you to stop charging up
            if (activityType == GameplayActivity.AttackedByEnemy || activityType == GameplayActivity.StoppedChargingUp)
            {
                StopChargingUp(serverCharacter);
            }
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            StopChargingUp(serverCharacter);

            // if stepped into invincibility, decrement invincibility counter
            if (Mathf.Approximately(GetPercentChargedUp(), 1f))
            {
                serverCharacter.serverAnimationHandler.NetworkAnimator.Animator.SetInteger(Config.OtherAnimatorVariable,
                    serverCharacter.serverAnimationHandler.NetworkAnimator.Animator.GetInteger(Config.OtherAnimatorVariable) - 1);
            }
        }

        private void StopChargingUp(ServerCharacter parent)
        {
            if (IsChargingUp())
            {
                m_StoppedChargingUpTime = Time.time;
                parent.clientCharacter.ClientStopChargingUpRpc(GetPercentChargedUp());

                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);

                parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Config.Anim);

                //tell the animator controller to enter "invincibility mode" (where we don't flinch from damage)
                if (Mathf.Approximately(GetPercentChargedUp(), 1f))
                {
                    // increment our "invincibility counter". We use an integer count instead of a boolean because the player
                    // can restart their shield before the first one has ended, thereby getting two stacks of invincibility.
                    // So each active copy of the charge-up increments the invincibility counter, and the animator controller
                    // knows anything greater than zero means we shouldn't show hit-reacts.
                    parent.serverAnimationHandler.NetworkAnimator.Animator.SetInteger(Config.OtherAnimatorVariable,
                        parent.serverAnimationHandler.NetworkAnimator.Animator.GetInteger(Config.OtherAnimatorVariable) + 1);
                }
            }
        }

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            Assert.IsTrue(Config.Spawns.Length == 2, $"Found {Config.Spawns.Length} spawns for action {name}. Should be exactly 2: a charge-up particle and a fully-charged particle");

            base.OnStartClient(clientCharacter);
            m_ChargeGraphics = InstantiateSpecialFXGraphic(Config.Spawns[0], clientCharacter.transform, true);
            return true;
        }
    }
}
