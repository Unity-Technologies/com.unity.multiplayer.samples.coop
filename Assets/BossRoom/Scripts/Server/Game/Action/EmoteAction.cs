using System;
using UnityEngine;

namespace BossRoom.Server
{
    public class EmoteAction : Action
    {
        public EmoteAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return false;
        }

        public override bool Update()
        {
            // since we return false at Start(), this method should not execute
            throw new InvalidOperationException("No logic defined.");
        }
    }
}
