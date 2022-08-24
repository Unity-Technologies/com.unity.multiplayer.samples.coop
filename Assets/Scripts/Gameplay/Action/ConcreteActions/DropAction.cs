using System;
using Unity.Multiplayer.Samples.BossRoom.Actions;
using Unity.Netcode;
using UnityEngine;
using Action = Unity.Multiplayer.Samples.BossRoom.Actions.Action;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Action for dropping "Heavy" items.
    /// </summary>
    public class DropAction : Action
    {
        float m_ActionStartTime;

        NetworkObject m_HeldNetworkObject;

        public DropAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool OnStart()
        {
            m_ActionStartTime = Time.time;

            // play animation of dropping a heavy object, if one is already held
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    m_Parent.NetState.heldNetworkObject.Value, out var heldObject))
            {
                m_HeldNetworkObject = heldObject;

                Data.TargetIds = null;

                if (!string.IsNullOrEmpty(Description.Anim))
                {
                    m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
                }
            }

            return true;
        }

        public override bool OnUpdate()
        {
            if (Time.time > m_ActionStartTime + Description.ExecTimeSeconds)
            {
                // drop the pot in space
                m_HeldNetworkObject.transform.SetParent(null);
                m_Parent.NetState.heldNetworkObject.Value = 0;

                return ActionConclusion.Stop;
            }

            return ActionConclusion.Continue;
        }
    }
}
