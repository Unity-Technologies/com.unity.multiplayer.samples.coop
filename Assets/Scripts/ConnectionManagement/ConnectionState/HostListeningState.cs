using System;
using System.Collections;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a listening host. Handles incoming client connections. When shutting down or
    /// being timed out, transitions to the Offline state.
    /// </summary>
    class HostListeningState : ServerListeningState
    {
        public override void OnUserRequestedShutdown()
        {
            ConnectionManager.SendServerToAllClientsSetDisconnectReason(ConnectStatus.HostEndedSession);
            // Wait before shutting down to make sure clients receive that message before they are disconnected
            m_ConnectionManager.StartCoroutine(WaitToShutdown());
        }

        IEnumerator WaitToShutdown()
        {
            yield return null;
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
            else
            {
                base.OnClientDisconnect(clientId);
            }
        }
    }
}
