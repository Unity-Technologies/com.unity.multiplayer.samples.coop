using MLAPI;
using System.Collections.Generic;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Server
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
        /// Cached reference to a component in Parent
        /// </summary>
        private ServerCharacterMovement m_Movement;

        /// <summary>
        /// Set once we've stopped charging up, for any reason:
        /// - the player has let go of the button,
        /// - we were attacked,
        /// - or the maximum charge was reached.
        /// </summary>
        private float m_StoppedChargingUpTime = 0;

        public ChargedShieldAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            m_Movement = m_Parent.GetComponent<ServerCharacterMovement>();

            if (m_Data.TargetIds != null && m_Data.TargetIds.Length > 0)
            {
                NetworkObject initialTarget = NetworkSpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
                if (initialTarget)
                {
                    // face our target, if we had one
                    m_Parent.transform.LookAt(initialTarget.transform.position);
                }
            }

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

            // we stop once the charge-up has ended and our effect duration has elapsed
            return m_StoppedChargingUpTime == 0 || Time.time < (m_StoppedChargingUpTime + Description.EffectDurationSeconds);
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return m_StoppedChargingUpTime != 0;
        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (buffType == BuffableValue.PercentDamageReceived)
            {
                float timeSpentChargingUp = m_StoppedChargingUpTime - TimeStarted;
                float pctChargedUp = Mathf.Clamp01(timeSpentChargingUp / Description.ExecTimeSeconds);

                // the amount of damage reduction starts at 50% (for not-charged-up), then slowly increases to 100% depending on how charged-up we got
                float pctDamageReduction = 0.5f + ((pctChargedUp * pctChargedUp) / 2);

                // Now that we know how much damage to reduce it by, we need to set buffedValue to the inverse (because
                // it's looking for how much damage to DO, not how much to REDUCE BY). Also note how we don't just SET
                // buffedValue... we multiply our buff in with the current value. This lets our Action "stack"
                // with any other Actions that also alter this variable.)
                buffedValue *= 1-pctDamageReduction;
            }
            else if (buffType == BuffableValue.ChanceToStunTramplers)
            {
                // if we are at "full charge", we stun enemies that try to trample us!
                float timeSpentChargingUp = m_StoppedChargingUpTime - TimeStarted;
                if (timeSpentChargingUp / Description.ExecTimeSeconds >= 1 && buffedValue < 1)
                {
                    buffedValue = 1;
                }
            }
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

        private void StopChargingUp()
        {
            if (m_StoppedChargingUpTime == 0)
            {
                m_StoppedChargingUpTime = Time.time;
                m_Parent.NetState.RecvStopChargingUpClientRpc();
            }
        }

    }
}
