using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public abstract class ConnectionState
    {
        public static OfflineConnectionState Offline { get; private set; }
        public static ConnectingConnectionState Connecting { get; private set; }
        public static ConnectedConnectionState Connected { get; private set; }
        public static ReconnectingConnectionState Reconnecting { get; private set; }
        public static HostingConnectionState Hosting { get; private set; }

        public static List<ConnectionState> States = new() { Offline, Connecting, Connected, Reconnecting, Hosting };

        public static void InitializeStates(ConnectionManager connectionManager, DIScope scope)
        {
            Offline = new OfflineConnectionState(connectionManager);
            Connecting = new ConnectingConnectionState(connectionManager);
            Connected = new ConnectedConnectionState(connectionManager);
            Reconnecting = new ReconnectingConnectionState(connectionManager);
            Hosting = new HostingConnectionState(connectionManager);

            foreach (var connectionState in States)
            {
                scope.InjectIn(connectionState);
            }
        }

        protected ConnectionManager m_ConnectionManager;

        protected ConnectionState(ConnectionManager connectionManager)
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

        public virtual void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback) {}
    }
}
