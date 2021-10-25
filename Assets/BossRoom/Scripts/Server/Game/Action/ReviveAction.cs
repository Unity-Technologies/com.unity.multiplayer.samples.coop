using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ReviveAction : Action
    {
        private bool m_ExecFired;
        private ServerCharacter m_TargetCharacter;

        public ReviveAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            if (m_Data.TargetIds == null || m_Data.TargetIds.Length == 0 || !NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(m_Data.TargetIds[0]))
            {
                Debug.Log("Failed to start ReviveAction. The target entity  wasn't submitted or doesn't exist anymore");
                return false;
            }

            var targetNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
            m_TargetCharacter = targetNetworkObject.GetComponent<ServerCharacter>();

            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);

            return true;
        }

        public override bool Update()
        {
            if (!m_ExecFired && Time.time - TimeStarted >= Description.ExecTimeSeconds)
            {
                m_ExecFired = true;

                if (m_TargetCharacter.NetState.LifeState == LifeState.Fainted)
                {
                    Assert.IsTrue(Description.Amount > 0, "Revive amount must be greater than 0.");
                    m_TargetCharacter.Revive(m_Parent, Description.Amount);
                }
                else
                {
                    //cancel the action if the target is alive!
                    Cancel();
                    return false;
                }
            }

            return true;
        }

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim2);
            }
        }
    }
}
