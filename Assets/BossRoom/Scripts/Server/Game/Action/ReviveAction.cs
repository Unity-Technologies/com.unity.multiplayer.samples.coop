using MLAPI;
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
            if (m_Data.TryGetSingleTarget(out NetworkedObject targetNetworkedObj))
            {
                m_TargetCharacter = targetNetworkedObj.GetComponent<ServerCharacter>();
                m_Parent.NetState.ServerBroadcastAction(ref Data);
                return true;
            }
            else
            {
                Debug.Log("Failed to start ReviveAction. The target entity  wasn't submitted or doesn't exist anymore");
                return false;
            }
        }

        public override bool Update()
        {
            if (!m_ExecFired && Time.time - TimeStarted >= Description.ExecTimeSeconds)
            {
                m_ExecFired = true;

                if (m_TargetCharacter.NetState.NetworkLifeState.Value == LifeState.Fainted)
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
