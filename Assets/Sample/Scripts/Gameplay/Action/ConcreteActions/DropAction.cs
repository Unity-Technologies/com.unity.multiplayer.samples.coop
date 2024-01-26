using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Action for dropping "Heavy" items.
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Drop Action")]
    public class DropAction : Action
    {
        float m_ActionStartTime;

        NetworkObject m_HeldNetworkObject;

        public override bool OnStart(ServerCharacter serverCharacter)
        {
            m_ActionStartTime = Time.time;

            // play animation of dropping a heavy object, if one is already held
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    serverCharacter.HeldNetworkObject.Value, out var heldObject))
            {
                m_HeldNetworkObject = heldObject;

                Data.TargetIds = null;

                if (!string.IsNullOrEmpty(Config.Anim))
                {
                    serverCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);
                }
            }

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            m_ActionStartTime = 0;
            m_HeldNetworkObject = null;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (Time.time > m_ActionStartTime + Config.ExecTimeSeconds)
            {
                // drop the pot in space
                m_HeldNetworkObject.transform.SetParent(null);
                clientCharacter.HeldNetworkObject.Value = 0;

                return ActionConclusion.Stop;
            }

            return ActionConclusion.Continue;
        }
    }
}
