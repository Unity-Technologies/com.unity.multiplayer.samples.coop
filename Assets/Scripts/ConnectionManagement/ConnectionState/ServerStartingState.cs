using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    class ServerStartingState : ConnectionState
    {
        public override void Enter()
        {
            var success = NetworkManager.Singleton.StartServer();

            // TODO do something with success != true
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ServerListening);
        }

        public override void Exit() { }
    }
}
