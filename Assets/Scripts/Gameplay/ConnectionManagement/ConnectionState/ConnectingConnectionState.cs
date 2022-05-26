using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ConnectingConnectionState : ConnectionState
    {
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPublisher;
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        public ConnectingConnectionState(ConnectionManager connectionManager, IPublisher<QuitGameSessionMessage> quitGameSessionPublisher,
            IPublisher<ConnectStatus> connectStatusPublisher)
            : base(connectionManager)
        {
            m_QuitGameSessionPublisher = quitGameSessionPublisher;
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        public override void OnClientConnected(ulong clientId)
        {
            m_ConnectionManager.ChangeState(ConnectionStateType.Connected);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            m_QuitGameSessionPublisher.Publish(new QuitGameSessionMessage(){UserRequested = false}); // go through the normal leave flow
            m_ConnectStatusPublisher.Publish(m_ConnectionManager.DisconnectReason.Reason);
            m_ConnectionManager.DisconnectReason.Clear();
            m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
        }

        public override void OnServerStarted()
        {
            throw new NotImplementedException();
        }

        public override void StartClientIP(string playerId, string playerName, string ipaddress, int port)
        {
            throw new NotImplementedException();
        }

        public override Task StartClientLobbyAsync(string playerName, string playerId, Action<string> onFailure)
        {
            throw new NotImplementedException();
        }

        public override bool StartHostIP(string playerId, string playerName, string ipaddress, int port)
        {
            throw new NotImplementedException();
        }

        public override Task StartHostLobbyAsync(string playerId, string playerName)
        {
            throw new NotImplementedException();
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
        }

        public override void OnServerShutdown()
        {
            throw new NotImplementedException();
        }

        public override void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            throw new NotImplementedException();
        }
    }
}
