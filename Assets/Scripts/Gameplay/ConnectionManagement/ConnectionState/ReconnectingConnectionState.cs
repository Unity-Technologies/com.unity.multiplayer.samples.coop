using System;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ReconnectingConnectionState : ConnectionState
    {
        public ReconnectingConnectionState(ConnectionManager connectionManager)
            : base(connectionManager)
        {
        }

        public override void OnClientConnected(ulong clientId)
        {
            throw new NotImplementedException();
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
