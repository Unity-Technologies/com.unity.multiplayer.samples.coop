using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.VisualEffects;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Causes the character to become hidden to enemies and other players. Notes:
    /// - Stealth starts after the ExecTimeSeconds has elapsed. If they are attacked during the Exec time, stealth is aborted.
    /// - Stealth ends when the player attacks or is damaged.
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Stealth Mode Action")]
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

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            serverCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            serverCharacter.clientCharacter.ClientPlayActionRpc(Data);

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            m_IsStealthEnded = false;
            m_IsStealthStarted = false;
            m_SpawnedGraphics = null;
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return TimeRunning >= Config.ExecTimeSeconds;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && !m_IsStealthStarted && !m_IsStealthEnded)
            {
                // start actual stealth-mode... NOW!
                m_IsStealthStarted = true;
                clientCharacter.IsStealthy.Value = true;
            }
            return !m_IsStealthEnded;
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                serverCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }

            EndStealth(serverCharacter);
        }

        public override void OnGameplayActivity(ServerCharacter serverCharacter, GameplayActivity activityType)
        {
            // we break stealth after using an attack. (Or after being hit, which could happen during exec time before we're stealthed, or even afterwards, such as from an AoE attack)
            if (activityType == GameplayActivity.UsingAttackAction || activityType == GameplayActivity.AttackedByEnemy)
            {
                EndStealth(serverCharacter);
            }
        }

        private void EndStealth(ServerCharacter parent)
        {
            if (!m_IsStealthEnded)
            {
                m_IsStealthEnded = true;
                if (m_IsStealthStarted)
                {
                    parent.IsStealthy.Value = false;
                }

                // note that we cancel the ActionFX here, and NOT in Cancel(). That's to handle the case where someone
                // presses the Stealth button twice in a row: "end this Stealth action and start a new one". If we cancelled
                // all actions of this type in Cancel(), we'd end up cancelling both the old AND the new one, because
                // the new one would already be in the clients' actionFX queue.
                parent.clientCharacter.ClientCancelActionsByPrototypeIDRpc(ActionID);
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            if (TimeRunning >= Config.ExecTimeSeconds && m_SpawnedGraphics == null && clientCharacter.IsOwner)
            {
                m_SpawnedGraphics = InstantiateSpecialFXGraphics(clientCharacter.transform, true);
            }

            return ActionConclusion.Continue;
        }

        public override void CancelClient(ClientCharacter clientCharacter)
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
