using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class ChargedShieldAction
    {
        /// <summary>
        /// The "charging up" graphics. These are disabled as soon as the player stops charging up
        /// </summary>
        SpecialFXGraphic m_ChargeGraphics;

        /// <summary>
        /// The "I'm fully charged" graphics. This is null until instantiated
        /// </summary>
        SpecialFXGraphic m_ShieldGraphics;

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return IsChargingUp() || (Time.time - m_StoppedChargingUpTime) < Config.EffectDurationSeconds;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
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

        public override void OnStoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage)
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
                m_ShieldGraphics = InstantiateSpecialFXGraphic(Config.Spawns[1], clientCharacter.transform, true);
            }
        }

        public override void AnticipateActionClient(ClientCharacter clientCharacter)
        {
            // because this action can be visually started and stopped as often and as quickly as the player wants, it's possible
            // for several copies of this action to be playing at once. This can lead to situations where several
            // dying versions of the action raise the end-trigger, but the animator only lowers it once, leaving the trigger
            // in a raised state. So we'll make sure that our end-trigger isn't raised yet. (Generally a good idea anyway.)
            clientCharacter.OurAnimator.ResetTrigger(Config.Anim2);
            base.AnticipateActionClient(clientCharacter);
        }
    }
}
