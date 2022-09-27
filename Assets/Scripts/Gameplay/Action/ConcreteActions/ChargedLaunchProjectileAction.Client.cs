using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class ChargedLaunchProjectileAction
    {
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

        public override bool OnStartClient(ClientCharacter clientCharacter)
        {
            base.OnStartClient(clientCharacter);

            m_Graphics = InstantiateSpecialFXGraphics(clientCharacter.transform, true);
            return true;
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return !m_ChargeEnded;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
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

        public override void OnStoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage)
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
