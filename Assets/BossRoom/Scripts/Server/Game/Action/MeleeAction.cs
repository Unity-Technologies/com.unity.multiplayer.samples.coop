using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    public class MeleeAction : Action
    {
        public MeleeAction(ServerCharacter parent, ref ActionRequestData data, int level) : base(parent, ref data, level)
        {
        }

        public override bool Start()
        {
            //stub. For now, just relay the action to all clients. 
            m_parent.NetState.S2C_BroadcastAction(ref Data);
            return true;
        }

        public override bool Update() { return true; }
    }
}
