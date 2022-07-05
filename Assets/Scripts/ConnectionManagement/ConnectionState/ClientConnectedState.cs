using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the
    /// ClientReconnecting state. When receiving a disconnect reason, transitions to the DisconnectingWithReason state.
    /// </summary>
    class ClientConnectedState : ConnectionState
    {
        [Inject]
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }

        public override void OnDisconnectReasonReceived(ConnectStatus disconnectReason)
        {
            m_ConnectStatusPublisher.Publish(disconnectReason);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_DisconnectingWithReason);
        }
    }
}
