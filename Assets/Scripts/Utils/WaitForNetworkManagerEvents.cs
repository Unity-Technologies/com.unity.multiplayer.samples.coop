using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Utilities
{
    public class WaitForServerStarted : CustomYieldInstruction
    {
        bool m_IsDone;

        public override bool keepWaiting => m_IsDone;

        public WaitForServerStarted()
        {
            void SetDone()
            {
                NetworkManager.Singleton.OnServerStarted -= SetDone;
                m_IsDone = true;
            }

            NetworkManager.Singleton.OnServerStarted += SetDone;
        }
    }
}
