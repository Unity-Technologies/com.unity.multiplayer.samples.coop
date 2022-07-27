using System;
using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Utilities
{
    /// <summary>
    /// Utility yield instruction allowing to wait for the few frames it takes to start a server
    /// This is less performant, as it actively polls each frames for the bool, but helps make server starting code more readable, by allowing to
    /// sequence pre-server startup code and post-server startup code without having to put it in a callback
    /// </summary>
    /// <remarks>TODO look into timeout</remarks>
    /// <example>
    /// IEnumerator StartServerCoroutine()
    /// {
    ///     NetworkManager.Singleton.StartServer();
    ///     yield return new WaitForServerStarted(); // Less performant than just the server started callback, but way more readable than a callback hell.
    ///     SceneLoaderWrapper.Instance.LoadScene(SceneNames.CharSelect, useNetworkSceneManager: true);
    /// }
    /// StartCoroutine(StartServerCoroutine());
    /// </example>
    public class WaitForServerStarted : CustomYieldInstruction
    {
        bool m_IsDone;
        NetworkManager m_NetworkManager;

        public override bool keepWaiting
        {
            get
            {
                if (m_NetworkManager.IsClient && !m_NetworkManager.IsServer) throw new NotServerException("shouldn't be called on clients");

                return !m_IsDone;
            }
        }

        public WaitForServerStarted() : this(NetworkManager.Singleton) { }

        public WaitForServerStarted(NetworkManager managerInstance)
        {
            m_NetworkManager = managerInstance;
            if (m_NetworkManager.IsServer)
            {
                m_IsDone = true;
                return;
            }

            void SetDone()
            {
                m_NetworkManager.OnServerStarted -= SetDone;
                m_IsDone = true;
            }

            m_NetworkManager.OnServerStarted += SetDone;
        }
    }
}
