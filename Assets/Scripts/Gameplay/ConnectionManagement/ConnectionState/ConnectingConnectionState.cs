using System;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.Utilities;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. If successful,transitions
    /// to the Connected state. If not, transitions to the Offline state.
    /// </summary>
    public class ConnectingConnectionState : ConnectionState
    {
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPublisher;

        [Inject]
        void InjectDependencies(IPublisher<QuitGameSessionMessage> quitGameSessionPublisher)
        {
            m_QuitGameSessionPublisher = quitGameSessionPublisher;
        }

        public override void Enter()
        {
            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
            m_ConnectionManager.RegisterCustomMessages();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong clientId)
        {
            m_ConnectionManager.ChangeState(Connected);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            m_QuitGameSessionPublisher.Publish(new QuitGameSessionMessage() { UserRequested = false }); // go through the normal leave flow
            m_ConnectionManager.ChangeState(Offline);
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(Offline);
        }
    }
}
