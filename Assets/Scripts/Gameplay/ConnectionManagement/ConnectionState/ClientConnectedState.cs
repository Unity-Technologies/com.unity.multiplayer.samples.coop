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
        ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;
        IDisposable m_Subscription;

        ConnectStatus m_ConnectStatus;

        [Inject]
        void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSubscriber)
        {
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
                    // go through the normal leave flow
                    m_ConnectionManager.ChangeState(Offline);
                    break;
                default:
                    // try reconnecting
                    m_ConnectionManager.ChangeState(ClientReconnecting);
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
