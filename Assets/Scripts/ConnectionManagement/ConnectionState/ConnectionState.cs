using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Base class representing a connection state. Contains a static member for each possible state.
    /// </summary>
    abstract class ConnectionState
    {
        [Inject]
        protected ConnectionManager m_ConnectionManager;

        [Inject]
        protected IPublisher<ConnectStatus> m_ConnectStatusPublisher;

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

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }
    }
}
