using MLAPI;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// The visual aspect of a ChargedLaunchProjectileAction.
    /// To show particles, the ActionDescription's Spawns list can provide a prefab that will be instantiated during run.
    /// The prefab must have a SpecialFXGraphic component on it, which is used to cleanly shut down the graphics.
    /// </summary>
    public class ChargedLaunchProjectileActionFX : ActionFX
    {
        public ChargedLaunchProjectileActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        /// <summary>
        /// A list of the special particle graphics we spawned.
        /// </summary>
        /// <remarks>
        /// Performance note: repeatedly creating and destroying GameObjects is not optimal, and on low-resource platforms
        /// (like mobile devices), it can lead to major performance problems. On mobile platforms, visual graphics should
        /// use object-pooling (i.e. reusing the same GameObjects repeatedly). But that's outside the scope of this demo.
        /// </remarks>
        private List<SpecialFXGraphic> m_Graphics;

        private bool m_ChargeEnded;

        public override bool Start()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);

            m_Graphics = InstantiateSpecialFXGraphics(true);
            return true;
        }

        public override bool Update()
        {
            return !m_ChargeEnded;
        }

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }

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

        public override void OnStoppedChargingUp(float finalChargeUpPercentage)
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
