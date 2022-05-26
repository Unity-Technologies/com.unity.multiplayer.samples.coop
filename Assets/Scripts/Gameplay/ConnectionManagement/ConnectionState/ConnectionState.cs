using System;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public abstract class ConnectionState
    {
        protected ConnectionManager m_ConnectionManager;

        public ConnectionState(ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }

        public virtual void OnClientConnected(ulong clientId) {}
        public virtual void OnClientDisconnect(ulong clientId) {}

        public virtual void OnServerStarted() {}

        public virtual void StartClientIP(string playerId, string playerName, string ipaddress, int port) {}

        public virtual Task StartClientLobbyAsync(string playerName, string playerId, Action<string> onFailure)
        {
            return Task.CompletedTask;
        }

        public virtual bool StartHostIP(string playerId, string playerName, string ipaddress, int port)
        {
            return false;
        }

        public virtual Task StartHostLobbyAsync(string playerId, string playerName)
        {
            return Task.CompletedTask;
        }

        public virtual void OnUserRequestedShutdown() {}

        public virtual void OnServerShutdown() {}

        public virtual void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback) {}
    }
}
