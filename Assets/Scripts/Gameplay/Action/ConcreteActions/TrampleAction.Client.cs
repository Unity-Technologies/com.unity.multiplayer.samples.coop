using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public partial class TrampleAction
    {
        /// <summary>
        /// We spawn the "visual cue" graphics a moment after we begin our action.
        /// (A little extra delay helps ensure we have the correct orientation for the
        /// character, so the graphics are oriented in the right direction!)
        /// </summary>
        private const float k_GraphicsSpawnDelay = 0.3f;

        /// <summary>
        /// Prior to spawning graphics, this is null. Once we spawn the graphics, this is a list of everything we spawned.
        /// </summary>
        /// <remarks>
        /// Mobile performance note: constantly creating new GameObjects like this has bad performance on mobile and should
        /// be replaced with object-pooling (i.e. reusing the same art GameObjects repeatedly). But that's outside the scope of this demo.
        /// </remarks>
        private List<SpecialFXGraphic> m_SpawnedGraphics = null;

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            float age = Time.time - TimeStarted;
            if (age > k_GraphicsSpawnDelay && m_SpawnedGraphics == null)
            {
                m_SpawnedGraphics = InstantiateSpecialFXGraphics(clientCharacter.transform, false);
            }

            return true;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
        {
            // we've been aborted -- destroy the "cue graphics"
            if (m_SpawnedGraphics != null)
            {
                foreach (var fx in m_SpawnedGraphics)
                {
                    if (fx)
                    {
                        fx.Shutdown();
                    }
                }
            }

            m_SpawnedGraphics = null;
        }
    }
}
