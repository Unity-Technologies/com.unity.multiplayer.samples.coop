using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public abstract class ConnectionState
    {
        public static readonly OfflineConnectionState Offline = new OfflineConnectionState();
        public static readonly ConnectingConnectionState Connecting = new ConnectingConnectionState();
        public static readonly ConnectedConnectionState Connected = new ConnectedConnectionState();
        public static readonly ReconnectingConnectionState Reconnecting = new ReconnectingConnectionState();
        public static readonly HostingConnectionState Hosting = new HostingConnectionState();

        public static readonly List<ConnectionState> States = new() { Offline, Connecting, Connected, Reconnecting, Hosting };

        public static void InitializeStates(ConnectionManager connectionManager, DIScope scope)
        {
            foreach (var connectionState in States)
            {
                connectionState.m_ConnectionManager = connectionManager;
                scope.InjectIn(connectionState);
            }
        }

        protected ConnectionManager m_ConnectionManager;

        public abstract void Enter();

        public abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) { }
        public virtual void OnClientDisconnect(ulong clientId) { }

        public virtual void StartClientIP(string playerName, string ipaddress, int port) { }

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

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback) {}
    }
}
