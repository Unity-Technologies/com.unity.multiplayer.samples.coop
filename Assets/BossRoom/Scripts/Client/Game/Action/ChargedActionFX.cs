using System;
using MLAPI;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// The visual aspect of a "Charged" action, including ChargedShieldAction and ChargedLaunchProjectileAction.
    /// To show particles, the ActionDescription's Spawns list can provide a prefab that will be instantiated during run.
    /// The prefab must have a SpecialFXGraphic component on it, which is used to cleanly shut down the graphics.
    /// </summary>
    public class ChargedActionFX : ActionFX
    {
        public ChargedActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

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

        public override bool Start()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);

            if (Description.Spawns.Length > 0)
            {
                foreach (var prefab in Description.Spawns)
                {
                    if (prefab && prefab.GetComponent<SpecialFXGraphic>()) // we skip any prefabs that aren't usable by us
                    {
                        var graphicsGO = GameObject.Instantiate(prefab, m_Parent.Parent.position, m_Parent.Parent.rotation, null);
                        var graphics = graphicsGO.GetComponent<SpecialFXGraphic>();
                        m_Graphics.Add(graphics);
                    }
                }
                if (m_Graphics.Count == 0)
                    throw new System.Exception($"None of the {Description.Spawns.Length} Spawns attached to {Description.ActionTypeEnum} have a SpecialFXGraphic component! No charge-up particles found!");
            }
            return true;
        }

        public override bool Update()
        {
            // make sure the particles stick near us! (Even if we aren't moving on the server, our visualization could still be moving for a bit.)
            // Note that we don't use the parent's rotation, because rotating the "charge up" particles just looks weird.
            foreach (var graphic in m_Graphics)
            {
                if (graphic)
                {
                    graphic.transform.position = m_Parent.Parent.position;
                }
            }

            return !m_ChargeEnded;
        }

        /// <summary>
        /// When this action is cancelled, we shutdown any graphics that have been created.
        /// </summary>
        public override void Cancel()
        {
            foreach (var graphic in m_Graphics)
            {
                if (graphic)
                {
                    graphic.Shutdown();
                }
            }

            m_Graphics.Clear();
        }

        /// <summary>
        /// This will fire when we've received a message from our character that our charge has ended (key released,
        /// action was interrupted, action cancelled).
        /// </summary>
        public override void OnStoppedChargingUp()
        {
            m_ChargeEnded = true;

            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }

            foreach (var graphic in m_Graphics)
            {
                if (graphic)
                {
                    graphic.End();
                }
            }
        }
    }
}
