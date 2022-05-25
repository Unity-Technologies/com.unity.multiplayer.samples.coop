using System;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ConnectingConnectionState : ConnectionState
    {
        public ConnectingConnectionState(ConnectionManager connectionManager)
            : base(connectionManager)
        {
        }

        public override void OnClientConnected(ulong clientId)
        {
            m_ConnectionManager.ChangeState(ConnectionStateType.Connected);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
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
