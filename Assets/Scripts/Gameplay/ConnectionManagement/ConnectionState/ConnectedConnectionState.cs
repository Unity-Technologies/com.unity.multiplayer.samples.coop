using System;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ConnectedConnectionState : ConnectionState
    {
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPublisher;
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        public ConnectedConnectionState(ConnectionManager connectionManager)
            : base(connectionManager) { }

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
            // This is also called on the Host when a different client disconnects. To make sure we only handle our own disconnection, verify that we are either
            // not a host (in which case we know this is about us) or that the clientID is the same as ours if we are the host.
            if (!NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsHost && NetworkManager.Singleton.LocalClientId == clientId)
            {
                //On a client disconnect we want to take them back to the main menu.
                //We have to check here in SceneManager if our active scene is the main menu, as if it is, it means we timed out rather than a raw disconnect;
                switch (m_ConnectionManager.DisconnectReason.Reason)
                {
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.HostEndedSession:
                        m_QuitGameSessionPublisher.Publish(new QuitGameSessionMessage() {UserRequested = false}); // go through the normal leave flow
                        m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
                        break;
                    default:
                        // try reconnecting
                        m_ConnectionManager.ChangeState(ConnectionStateType.Reconnecting);
                        break;
                }

                m_ConnectStatusPublisher.Publish(m_ConnectionManager.DisconnectReason.Reason);
                m_ConnectionManager.DisconnectReason.Clear();
            }
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
        }
    }
}
