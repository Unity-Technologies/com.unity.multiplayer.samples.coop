using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// The visual aspect of a ChargedShieldAction. Shows "charge up particles" while the power is charging up.
    /// If charge-up reaches maximum, we show a separate "shielded" graphic for EffectDurationSeconds. During that
    /// time we also disable hit-reactions so that the character doesn't appear to "flinch" (since they
    /// aren't taking any damage!)
    /// </summary>
    public class ChargedShieldActionFX : ActionFX
    {
        /// <summary>
        /// The Time.time when we stop "charging up", or 0 if we haven't stopped yet.
        /// </summary>
        float m_StoppedChargingUpTime = 0;

        /// <summary>
        /// The "charging up" graphics. These are disabled as soon as the player stops charging up
        /// </summary>
        SpecialFXGraphic m_ChargeGraphics;

        /// <summary>
        /// The "I'm fully charged" graphics. This is null until instantiated
        /// </summary>
        SpecialFXGraphic m_ShieldGraphics;

        public ChargedShieldActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        public override bool Start()
        {
            Assert.IsTrue(Description.Spawns.Length == 2, $"Found {Description.Spawns.Length} spawns for action {Description.ActionTypeEnum}. Should be exactly 2: a charge-up particle and a fully-charged particle");

            base.Start();
            m_ChargeGraphics = InstantiateSpecialFXGraphic(Description.Spawns[0], m_Parent.transform, true);
            return true;
        }

        private bool IsChargingUp()
        {
            return m_StoppedChargingUpTime == 0;
        }

        public override bool Update()
        {
            return IsChargingUp() || (Time.time - m_StoppedChargingUpTime) < Description.EffectDurationSeconds;
        }

        public override void Cancel()
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

        public override void OnStoppedChargingUp(float finalChargeUpPercentage)
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
                m_ShieldGraphics = InstantiateSpecialFXGraphic(Description.Spawns[1], m_Parent.transform, true);
            }
        }

        public override void AnticipateAction()
        {
            // because this action can be visually started and stopped as often and as quickly as the player wants, it's possible
            // for several copies of this action to be playing at once. This can lead to situations where several
            // dying versions of the action raise the end-trigger, but the animator only lowers it once, leaving the trigger
            // in a raised state. So we'll make sure that our end-trigger isn't raised yet. (Generally a good idea anyway.)
            m_Parent.OurAnimator.ResetTrigger(Description.Anim2);
            base.AnticipateAction();
        }
    }
}

