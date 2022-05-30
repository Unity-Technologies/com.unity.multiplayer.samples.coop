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

        public abstract void Enter();

        public abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) {}
        public virtual void OnClientDisconnect(ulong clientId) {}

        public virtual void StartClientIP(string playerName, string ipaddress, int port) {}

        public virtual Task StartClientLobbyAsync(string playerName, Action<string> onFailure)
        {
            return Task.CompletedTask;
        }

        public virtual bool StartHostIP(string playerName, string ipaddress, int port)
        {
            return false;
        }

        public virtual Task StartHostLobbyAsync(string playerName)
        {
            return Task.CompletedTask;
        }

        public virtual void OnUserRequestedShutdown() {}

        public virtual void OnServerShutdown() {}

        public virtual void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback) {}
    }
}
