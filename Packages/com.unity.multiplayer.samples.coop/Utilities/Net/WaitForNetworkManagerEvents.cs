using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Utilities
{
    /// <summary>
    /// Utility yield instruction allowing to wait for the few frames it takes to start a server
    /// This is less performant, as it actively polls each frames for the bool, but helps make server starting code more readable, by allowing to
    /// sequence pre-server startup code and post-server startup code without having to put it in a callback
    /// </summary>
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
