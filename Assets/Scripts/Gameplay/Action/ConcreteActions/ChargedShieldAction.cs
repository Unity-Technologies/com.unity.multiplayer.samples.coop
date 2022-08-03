using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
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
    public class ChargedShieldAction : Action
    {
        /// <summary>
        /// Set once we've stopped charging up, for any reason:
        /// - the player has let go of the button,
        /// - we were attacked,
        /// - or the maximum charge was reached.
        /// </summary>
        private float m_StoppedChargingUpTime = 0;

        /// <summary>
        /// The "charging up" graphics. These are disabled as soon as the player stops charging up
        /// </summary>
        SpecialFXGraphic m_ChargeGraphics;

        /// <summary>
        /// The "I'm fully charged" graphics. This is null until instantiated
        /// </summary>
        SpecialFXGraphic m_ShieldGraphics;

        public ChargedShieldAction( ref ActionRequestData data)
            : base( ref data) { }

        public override bool OnStart(ServerCharacter parent)
        {
            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkObject initialTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    // face our target, if we had one
                    parent.physicsWrapper.Transform.LookAt(initialTarget.transform.position);
                }
            }

            // because this action can be visually started and stopped as often and as quickly as the player wants, it's possible
            // for several copies of this action to be playing at once. This can lead to situations where several
            // dying versions of the action raise the end-trigger, but the animator only lowers it once, leaving the trigger
            // in a raised state. So we'll make sure that our end-trigger isn't raised yet. (Generally a good idea anyway.)
            parent.serverAnimationHandler.NetworkAnimator.ResetTrigger(Description.Anim2);

            // raise the start trigger to start the animation loop!
            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);

            parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        private bool IsChargingUp()
        {
            return m_StoppedChargingUpTime == 0;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (m_StoppedChargingUpTime == 0)
            {
                // we haven't explicitly stopped charging up... but if we've reached max charge, that implicitly stops us
                if (TimeRunning >= Description.ExecTimeSeconds)
                {
                    StopChargingUp(parent);
                }
            }

            // we stop once the charge-up has ended and our effect duration has elapsed
            return m_StoppedChargingUpTime == 0 || Time.time < (m_StoppedChargingUpTime + Description.EffectDurationSeconds);
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return m_StoppedChargingUpTime != 0;
        }

        private float GetPercentChargedUp()
        {
            return ActionUtils.GetPercentChargedUp(m_StoppedChargingUpTime, TimeRunning, TimeStarted, Description.ExecTimeSeconds);
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

        public override void OnGameplayActivity(ServerCharacter parent, GameplayActivity activityType)
        {
            // for this particular type of Action, being attacked immediately causes you to stop charging up
            if (activityType == GameplayActivity.AttackedByEnemy || activityType == GameplayActivity.StoppedChargingUp)
            {
                StopChargingUp(parent);
            }
        }

        public override void Cancel(ServerCharacter parent)
        {
            StopChargingUp(parent);

            // if stepped into invincibility, decrement invincibility counter
            if (Mathf.Approximately(GetPercentChargedUp(), 1f))
            {
                parent.serverAnimationHandler.NetworkAnimator.Animator.SetInteger(Description.OtherAnimatorVariable,
                    parent.serverAnimationHandler.NetworkAnimator.Animator.GetInteger(Description.OtherAnimatorVariable) - 1);
            }
        }

        private void StopChargingUp(ServerCharacter parent)
        {
            if (IsChargingUp())
            {
                m_StoppedChargingUpTime = Time.time;
                parent.NetState.RecvStopChargingUpClientRpc(GetPercentChargedUp());

                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);

                //tell the animator controller to enter "invincibility mode" (where we don't flinch from damage)
                if (Mathf.Approximately(GetPercentChargedUp(), 1f))
                {
                    // increment our "invincibility counter". We use an integer count instead of a boolean because the player
                    // can restart their shield before the first one has ended, thereby getting two stacks of invincibility.
                    // So each active copy of the charge-up increments the invincibility counter, and the animator controller
                    // knows anything greater than zero means we shouldn't show hit-reacts.
                    parent.serverAnimationHandler.NetworkAnimator.Animator.SetInteger(Description.OtherAnimatorVariable,
                        parent.serverAnimationHandler.NetworkAnimator.Animator.GetInteger(Description.OtherAnimatorVariable) + 1);
                }
            }
        }

        public override bool OnStartClient(ClientCharacterVisualization parent)
        {
            Assert.IsTrue(Description.Spawns.Length == 2, $"Found {Description.Spawns.Length} spawns for action {Description.ActionTypeEnum}. Should be exactly 2: a charge-up particle and a fully-charged particle");

            base.OnStartClient(parent);
            m_ChargeGraphics = InstantiateSpecialFXGraphic(Description.Spawns[0], parent.transform, true);
            return true;
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            return IsChargingUp() || (Time.time - m_StoppedChargingUpTime) < Description.EffectDurationSeconds;
        }

        public override void CancelClient(ClientCharacterVisualization parent)
        {
            if (IsChargingUp())
            {
                // we never actually stopped "charging up" so do necessary clean up here
                if (m_ChargeGraphics)
                {
                    m_ChargeGraphics.Shutdown();
                }
            }

            if (m_ShieldGraphics)
            {
                m_ShieldGraphics.Shutdown();
            }
        }

        public override void OnStoppedChargingUpClient(ClientCharacterVisualization parent, float finalChargeUpPercentage)
        {
            if (!IsChargingUp()) { return; }

            m_StoppedChargingUpTime = Time.time;
            if (m_ChargeGraphics)
            {
                m_ChargeGraphics.Shutdown();
                m_ChargeGraphics = null;
            }
            // if fully charged, we show a special graphic
            if (Mathf.Approximately(finalChargeUpPercentage, 1))
            {
                m_ShieldGraphics = InstantiateSpecialFXGraphic(Description.Spawns[1], parent.transform, true);
            }
        }

        public override void AnticipateActionClient(ClientCharacterVisualization parent)
        {
            // because this action can be visually started and stopped as often and as quickly as the player wants, it's possible
            // for several copies of this action to be playing at once. This can lead to situations where several
            // dying versions of the action raise the end-trigger, but the animator only lowers it once, leaving the trigger
            // in a raised state. So we'll make sure that our end-trigger isn't raised yet. (Generally a good idea anyway.)
            parent.OurAnimator.ResetTrigger(Description.Anim2);
            base.AnticipateActionClient(parent);
        }
    }
}
