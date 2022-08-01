using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    class ServerStartingState : ConnectionState
    {
        public override void Enter()
        {
            var success = NetworkManager.Singleton.StartServer();
            if (!success)
            {
                DedicatedServerUtilities.LogCustom("StartServer returned false and failed starting. Killing process.");
                Application.Quit(1); // "1" exit code to tell whatever is running this server something wrong happened.
                return;
            }

            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ServerListening);
        }

        public override void Exit() { }
    }
}
