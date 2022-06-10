using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected or timed out, transitions to the
    /// Offline or Reconnecting state, depending on the cause of the disconnect.
    /// </summary>
    public class ClientConnectedState : ConnectionState
    {
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        [Inject]
        void InjectDependencies(IPublisher<ConnectStatus> connectStatusPublisher)
        {
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
            m_ConnectionManager.ChangeState(ClientReconnecting);
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            m_ConnectionManager.ChangeState(Offline);
        }

        public override void OnDisconnectReasonReceived(ConnectStatus disconnectReason)
        {
            m_ConnectStatusPublisher.Publish(disconnectReason);
            m_ConnectionManager.ChangeState(DisconnectingWithReason);
        }
    }
}
