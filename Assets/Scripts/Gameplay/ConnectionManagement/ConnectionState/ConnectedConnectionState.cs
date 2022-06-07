using System;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ConnectedConnectionState : ConnectionState
    {
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPublisher;
        ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;
        IDisposable m_Subscription;

        ConnectStatus m_ConnectStatus;

        [Inject]
        void InjectDependencies(IPublisher<QuitGameSessionMessage> quitGameSessionPublisher, ISubscriber<ConnectStatus> connectStatusSubscriber)
        {
            m_QuitGameSessionPublisher = quitGameSessionPublisher;
            m_ConnectStatusSubscriber = connectStatusSubscriber;
        }

        public override void Enter()
        {
            m_Subscription = m_ConnectStatusSubscriber.Subscribe(status => m_ConnectStatus = status);
        }

        public override void Exit()
        {
            m_Subscription.Dispose();
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            switch (m_ConnectStatus)
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
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(Offline);
        }
    }
}
