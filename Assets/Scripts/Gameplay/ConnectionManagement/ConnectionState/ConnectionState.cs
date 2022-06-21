using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Base class representing a connection state. Contains a static member for each possible state.
    /// </summary>
    public abstract class ConnectionState
    {
        public static readonly OfflineState Offline = new OfflineState();
        public static readonly ClientConnectingState ClientConnecting = new ClientConnectingState();
        public static readonly ClientConnectedState ClientConnected = new ClientConnectedState();
        public static readonly ClientReconnectingState ClientReconnecting = new ClientReconnectingState();
        public static readonly DisconnectingWithReasonState DisconnectingWithReason = new DisconnectingWithReasonState();
        public static readonly StartingHostState StartingHost = new StartingHostState();
        public static readonly HostingState Hosting = new HostingState();

        public static readonly List<ConnectionState> States = new() { Offline, ClientConnecting, ClientConnected, ClientReconnecting, DisconnectingWithReason, StartingHost, Hosting };

        /// <summary>
        /// Initializes each of the static states, by setting the reference to the ConnectionManager and injecting dependencies.
        /// </summary>
        /// <param name="connectionManager"></param>
        /// <param name="scope"></param>
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

        public virtual void OnServerStarted() { }

        public virtual void StartClientIP(string playerName, string ipaddress, int port) { }

        public virtual void StartClientLobby(string playerName) { }

        public virtual void StartHostIP(string playerName, string ipaddress, int port) { }

        public virtual void StartHostLobby(string playerName) { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void OnDisconnectReasonReceived(ConnectStatus disconnectReason) { }

        public virtual void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback) { }
    }
}
