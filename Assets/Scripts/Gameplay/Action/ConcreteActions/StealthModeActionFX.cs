using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Visual;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
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

        public StealthModeActionFX(ref ActionRequestData data, ClientCharacterVisualization clientParent) : base(ref data, clientParent) { }

        public override bool OnUpdateClient()
        {
            if (c_TimeRunning >= c_Description.ExecTimeSeconds && m_SpawnedGraphics == null && m_ClientParent.IsOwner)
            {
                m_SpawnedGraphics = InstantiateSpecialFXGraphicsClient(m_ClientParent.transform, true);
            }

            return ActionConclusion.Continue;
        }

        public override void CancelClient()
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
        }
    }
}
