using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Graphics for the rogue's StealthModeAction. Note that this is only part of the visual-effects for this action!
    /// The ClientCharacterVisualization is also involved: it hides the actual character model from other players.
    /// That means our job here is just to:
    /// 
    /// - play animations
    /// - show a particle effect, but only for the player that owns this character! (Because the other players can't see
    ///   the character, and showing a particle effect where they're standing would be a dead giveaway.)
    ///
    /// Since StealthModeAction has no finite duration, we keep running until canceled by the server!
    /// </summary>
    public class StealthModeActionFX : ActionFX
    {
        /// <summary>
        /// When non-null, a list of all graphics spawned.
        /// (If null, means we haven't been running long enough yet, or we aren't using any graphics because we're invisible on this client)
        /// These are created from the Description.Spawns list. Each prefab in that list should have a SpecialFXGraphic component.
        /// </summary>
        private List<SpecialFXGraphic> m_SpawnedGraphics = null;

        public StealthModeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        public override bool Start()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
            return true;
        }

        public override bool Update()
        {
            if (TimeRunning >= Description.ExecTimeSeconds && m_SpawnedGraphics == null && m_Parent.IsOwner)
            {
                m_SpawnedGraphics = new List<SpecialFXGraphic>();
                foreach (var prefab in Description.Spawns)
                {
                    var specialEffectsGO = GameObject.Instantiate(prefab, m_Parent.transform);
                    var specialEffect = specialEffectsGO.GetComponent<SpecialFXGraphic>();
                    if (!specialEffect)
                        throw new System.Exception($"{Description.ActionTypeEnum} has a spawned graphic that does not have a SpecialFXGraphic component!");
                    m_SpawnedGraphics.Add(specialEffect);
                }
            }

            return ActionConclusion.Continue;
        }

        public override void Cancel()
        {
            if (m_SpawnedGraphics != null)
            {
                foreach (var graphic in m_SpawnedGraphics)
                {
                    if (graphic)
                    {
                        graphic.transform.SetParent(null);
                        graphic.Shutdown();
                    }
                }
            }

            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }
        }
    }
}
