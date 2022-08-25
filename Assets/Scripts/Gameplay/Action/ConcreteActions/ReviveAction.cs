using System;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    [CreateAssetMenu(menuName = "BossRoom/Actions/Revive Action")]
    public class ReviveAction : Action
    {
        private bool m_ExecFired;
        private ServerCharacter m_TargetCharacter;

        public override bool OnStart(ServerCharacter parent)
        {
            if (m_Data.TargetIds == null || m_Data.TargetIds.Length == 0 || !NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(m_Data.TargetIds[0]))
            {
                Debug.Log("Failed to start ReviveAction. The target entity  wasn't submitted or doesn't exist anymore");
                return false;
            }

            var targetNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
            m_TargetCharacter = targetNetworkObject.GetComponent<ServerCharacter>();

            parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim);

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            m_ExecFired = false;
            m_TargetCharacter = null;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            if (!m_ExecFired && Time.time - TimeStarted >= Config.ExecTimeSeconds)
            {
                m_ExecFired = true;

                if (m_TargetCharacter.NetState.LifeState == LifeState.Fainted)
                {
                    Assert.IsTrue(Config.Amount > 0, "Revive amount must be greater than 0.");
                    m_TargetCharacter.Revive(parent, Config.Amount);
                }
                else
                {
                    //cancel the action if the target is alive!
                    Cancel(parent);
                    return false;
                }
            }

            return true;
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (!string.IsNullOrEmpty(Config.Anim2))
            {
                parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Config.Anim2);
            }
        }
    }
}
