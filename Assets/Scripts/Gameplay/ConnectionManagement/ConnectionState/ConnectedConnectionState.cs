using System;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ConnectedConnectionState : ConnectionState
    {
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPublisher;
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        [Inject]
        void InjectDependencies(IPublisher<QuitGameSessionMessage> quitGameSessionPublisher,
            IPublisher<ConnectStatus> connectStatusPublisher)
        {
            m_QuitGameSessionPublisher = quitGameSessionPublisher;
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            switch (m_ConnectionManager.DisconnectReason.Reason)
            {
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.HostEndedSession:
                    m_QuitGameSessionPublisher.Publish(new QuitGameSessionMessage() { UserRequested = false }); // go through the normal leave flow
                    m_ConnectionManager.ChangeState(Offline);
                    break;
                default:
                    // try reconnecting
                    m_ConnectionManager.ChangeState(Reconnecting);
                    break;
            }

            m_ConnectStatusPublisher.Publish(m_ConnectionManager.DisconnectReason.Reason);
            m_ConnectionManager.DisconnectReason.Clear();
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(Offline);
        }
    }
}
