using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.Utilities;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering If successful, transitions to the Connected state. If not, transitions to the Offline state.
    /// </summary>
    public class ClientConnectingState : ConnectionState
    {
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        [Inject]
        void InjectDependencies(IPublisher<ConnectStatus> connectStatusPublisher)
        {
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        public override void Enter()
        {
            if (m_ConnectionManager.NetworkManager.StartClient())
            {
                SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
                m_ConnectionManager.RegisterCustomMessages();
            }
            else
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
                m_ConnectionManager.ChangeState(Offline);
            }
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong _)
        {
            m_ConnectionManager.ChangeState(ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
            m_ConnectionManager.ChangeState(Offline);
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
