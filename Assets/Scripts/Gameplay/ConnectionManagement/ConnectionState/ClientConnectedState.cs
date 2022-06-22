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

        public override void OnClientDisconnect(ulong _)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
            StateChangeRequest?.Invoke(ClientReconnecting);
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            StateChangeRequest?.Invoke(Offline);
        }

        public override void OnDisconnectReasonReceived(ConnectStatus disconnectReason)
        {
            m_ConnectStatusPublisher.Publish(disconnectReason);
            StateChangeRequest?.Invoke(DisconnectingWithReason);
        }
    }
}
