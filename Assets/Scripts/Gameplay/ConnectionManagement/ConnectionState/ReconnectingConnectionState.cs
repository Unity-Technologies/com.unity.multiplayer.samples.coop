using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ReconnectingConnectionState : ConnectionState
    {
        const int k_NbReconnectAttempts = 2;

        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;
        IPublisher<ReconnectMessage> m_ReconnectMessagePublisher;

        Coroutine m_ReconnectCoroutine;
        string m_LobbyCode = "";
        int m_NbAttempts;

        public ReconnectingConnectionState(ConnectionManager connectionManager, LobbyServiceFacade lobbyServiceFacade,
            LocalLobby localLobby, IPublisher<ReconnectMessage> reconnectMessagePublisher)
            : base(connectionManager)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_ReconnectMessagePublisher = reconnectMessagePublisher;
        }

        public override void Enter()
        {
            m_LobbyCode = m_LobbyServiceFacade.CurrentUnityLobby != null ? m_LobbyServiceFacade.CurrentUnityLobby.LobbyCode : "";
            m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
            m_NbAttempts = 0;
        }

        public override void Exit()
        {
            if (m_ReconnectCoroutine != null)
            {
                m_ConnectionManager.StopCoroutine(m_ReconnectCoroutine);
                m_ReconnectCoroutine = null;
                m_ReconnectMessagePublisher.Publish(new ReconnectMessage(k_NbReconnectAttempts, k_NbReconnectAttempts));
            }
        }

        public override void OnClientConnected(ulong clientId)
        {
            m_ConnectionManager.ChangeState(ConnectionStateType.Connected);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            switch (m_ConnectionManager.DisconnectReason.Reason)
            {
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.HostEndedSession:
                case ConnectStatus.ServerFull:
                    m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
                    break;
                default:
                    m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
                    break;
            }
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
        }

        IEnumerator ReconnectCoroutine()
        {
            Debug.Log("Lost connection to host, trying to reconnect...");

            NetworkManager.Singleton.Shutdown();

            yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress); // wait until NetworkManager completes shutting down
            Debug.Log($"Reconnecting attempt {m_NbAttempts + 1}/{k_NbReconnectAttempts}...");
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(m_NbAttempts, k_NbReconnectAttempts));
            if (!string.IsNullOrEmpty(m_LobbyCode))
            {
                var leavingLobby = m_LobbyServiceFacade.EndTracking();
                yield return new WaitUntil(() => leavingLobby.IsCompleted);
                var joiningLobby = m_LobbyServiceFacade.TryJoinLobbyAsync("", m_LobbyCode);
                yield return new WaitUntil(() => joiningLobby.IsCompleted);
                if (joiningLobby.Result.Success)
                {
                    m_LobbyServiceFacade.SetRemoteLobby(joiningLobby.Result.Lobby);
                    var joiningRelay = JoinRelayServerAsync();
                    yield return new WaitUntil(() => joiningRelay.IsCompleted);
                }
                else
                {
                    Debug.Log("Failed joining lobby.");
                }
            }
            else
            {
                ConnectClient();
            }

            m_NbAttempts++;
        }

        async Task JoinRelayServerAsync()
        {
            try
            {
                var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) =
                    await UnityRelayUtilities.JoinRelayServerFromJoinCode(m_LocalLobby.RelayJoinCode);

                await m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(allocationIdBytes.ToString(), m_LocalLobby.RelayJoinCode);
                var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
                utp.SetClientRelayData(ipv4Address, port, allocationIdBytes, key, connectionData, hostConnectionData, isSecure: true);
            }
            catch (Exception)
            {
                return;//not re-throwing, but still not allowing to connect
            }

            ConnectClient();
        }

        void ConnectClient()
        {
            //and...we're off! Netcode will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by getting an ReceiveServerToClientSetDisconnectReason_CustomMessage callback for ourselves and get a message telling us the reason
            //  If the socket connection succeeds, we'll get our ReceiveServerToClientConnectResult_CustomMessage invoked. This is where game-layer failures will be reported.
            m_ConnectionManager.NetworkManager.StartClient();
            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
            m_ConnectionManager.RegisterCustomMessages();
        }
    }
}