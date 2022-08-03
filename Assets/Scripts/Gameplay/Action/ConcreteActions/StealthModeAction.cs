using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Causes the character to become hidden to enemies and other players. Notes:
    /// - Stealth starts after the ExecTimeSeconds has elapsed. If they are attacked during the Exec time, stealth is aborted.
    /// - Stealth ends when the player attacks or is damaged.
    /// </summary>
    public class StealthModeAction : Action
    {
        private bool m_IsStealthStarted = false;
        private bool m_IsStealthEnded = false;

        /// <summary>
        /// When non-null, a list of all graphics spawned.
        /// (If null, means we haven't been running long enough yet, or we aren't using any graphics because we're invisible on this client)
        /// These are created from the Description.Spawns list. Each prefab in that list should have a SpecialFXGraphic component.
        /// </summary>
        private List<SpecialFXGraphic> m_SpawnedGraphics = null;

        public StealthModeAction(ref ActionRequestData data) : base(ref data) { }

        public override bool OnStart(ServerCharacter parent)
        {
            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);

            parent.NetState.RecvDoActionClientRPC(Data);

            return true;
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return TimeRunning >= Description.ExecTimeSeconds;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (TimeRunning >= Description.ExecTimeSeconds && !m_IsStealthStarted && !m_IsStealthEnded)
            {
                // start actual stealth-mode... NOW!
                m_IsStealthStarted = true;
                parent.NetState.IsStealthy.Value = true;
            }
            return !m_IsStealthEnded;
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }

            EndStealth(parent);
        }

        public override void OnGameplayActivity(ServerCharacter parent, GameplayActivity activityType)
        {
            // we break stealth after using an attack. (Or after being hit, which could happen during exec time before we're stealthed, or even afterwards, such as from an AoE attack)
            if (activityType == GameplayActivity.UsingAttackAction || activityType == GameplayActivity.AttackedByEnemy)
            {
                EndStealth(parent);
            }
        }

        private void EndStealth(ServerCharacter parent)
        {
            if (!m_IsStealthEnded)
            {
                m_IsStealthEnded = true;
                if (m_IsStealthStarted)
                {
                    parent.NetState.IsStealthy.Value = false;
                }

                // note that we cancel the ActionFX here, and NOT in Cancel(). That's to handle the case where someone
                // presses the Stealth button twice in a row: "end this Stealth action and start a new one". If we cancelled
                // all actions of this type in Cancel(), we'd end up cancelling both the old AND the new one, because
                // the new one would already be in the clients' actionFX queue.
                parent.NetState.RecvCancelActionsByTypeClientRpc(Description.ActionTypeEnum);
            }
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            if (TimeRunning >= Description.ExecTimeSeconds && m_SpawnedGraphics == null && parent.IsOwner)
            {
                m_SpawnedGraphics = InstantiateSpecialFXGraphics(parent.transform, true);
            }

            return ActionConclusion.Continue;
        }

        public override void CancelClient(ClientCharacterVisualization parent)
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
