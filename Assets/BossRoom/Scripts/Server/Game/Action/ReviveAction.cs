using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Server
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
            if (m_Data.TargetIds == null || m_Data.TargetIds.Length == 0 || !NetworkSpawnManager.SpawnedObjects.ContainsKey(m_Data.TargetIds[0]))
            {
                Debug.Log("Failed to start ReviveAction. The target entity  wasn't submitted or doesn't exist anymore");
                return false;
            }

            var targetNeworkedObj = NetworkSpawnManager.SpawnedObjects[m_Data.TargetIds[0]];
            m_TargetCharacter = targetNeworkedObj.GetComponent<ServerCharacter>();
            m_Parent.NetState.RecvDoActionClientRPC(Data);

            return true;
        }

        public override bool Update()
        {
            if (!m_ExecFired && Time.time - TimeStarted >= Description.ExecTimeSeconds)
            {
                m_ExecFired = true;

                if (m_TargetCharacter.NetState.LifeState == LifeState.Fainted)
                {
                    m_TargetCharacter.Revive(m_Parent, (int)m_Data.Amount);
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
    }
}
