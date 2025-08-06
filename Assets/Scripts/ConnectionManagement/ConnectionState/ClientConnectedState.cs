using Unity.BossRoom.UnityServices.Sessions;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the
    /// ClientReconnecting state if no reason is given, or to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        [Inject]
        protected MultiplayerServicesFacade m_MultiplayerServicesFacade;

        public override void Enter()
        {
            if (m_MultiplayerServicesFacade.CurrentUnitySession != null)
            {
                m_MultiplayerServicesFacade.BeginTracking();
            }
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason) ||
                disconnectReason == "Disconnected due to host shutting down.")
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                m_ConnectStatusPublisher.Publish(connectStatus);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }
    }
}
