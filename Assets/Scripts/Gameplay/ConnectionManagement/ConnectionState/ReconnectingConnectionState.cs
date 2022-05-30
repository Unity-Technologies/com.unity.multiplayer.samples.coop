using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ReconnectingConnectionState : OfflineConnectionState
    {
        const int k_NbReconnectAttempts = 2;

        IPublisher<ReconnectMessage> m_ReconnectMessagePublisher;

        Coroutine m_ReconnectCoroutine;
        string m_LobbyCode = "";
        int m_NbAttempts;

        public ReconnectingConnectionState(ConnectionManager connectionManager, LobbyServiceFacade lobbyServiceFacade,
            LocalLobby localLobby, IPublisher<ReconnectMessage> reconnectMessagePublisher, ProfileManager profileManager)
            : base(connectionManager, lobbyServiceFacade, localLobby, profileManager)
        {
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
            }
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(k_NbReconnectAttempts, k_NbReconnectAttempts));
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
                    if (m_NbAttempts < k_NbReconnectAttempts)
                    {
                        m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
                    }
                    else
                    {
                        m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
                    }
                    break;
            }
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.ChangeState(ConnectionStateType.Offline);
        }

        public override void StartClientIP(string playerName, string ipaddress, int port) {}

        public override Task StartClientLobbyAsync(string playerName, Action<string> onFailure)
        {
            return Task.CompletedTask;
        }

        public override bool StartHostIP(string playerName, string ipaddress, int port)
        {
            return false;
        }

        public override Task StartHostLobbyAsync(string playerName)
        {
            return Task.CompletedTask;
        }

        IEnumerator ReconnectCoroutine()
        {
            Debug.Log("Lost connection to host, trying to reconnect...");

            NetworkManager.Singleton.Shutdown();

            yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress); // wait until NetworkManager completes shutting down
            Debug.Log($"Reconnecting attempt {m_NbAttempts + 1}/{k_NbReconnectAttempts}...");
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(m_NbAttempts, k_NbReconnectAttempts));
            m_NbAttempts++;
            if (!string.IsNullOrEmpty(m_LobbyCode))
            {
                var leavingLobby = m_LobbyServiceFacade.EndTracking();
                yield return new WaitUntil(() => leavingLobby.IsCompleted);
                var joiningLobby = m_LobbyServiceFacade.TryJoinLobbyAsync("", m_LobbyCode);
                yield return new WaitUntil(() => joiningLobby.IsCompleted);
                if (joiningLobby.Result.Success)
                {
                    m_LobbyServiceFacade.SetRemoteLobby(joiningLobby.Result.Lobby);
                    var joiningRelay = JoinRelayServerAsync(null);
                    yield return new WaitUntil(() => joiningRelay.IsCompleted);
                    if (joiningRelay.Result)
                    {
                        ConnectClient();
                    }
                    else
                    {
                        Debug.Log("Failed joining Relay server.");
                        OnClientDisconnect(0);
                    }
                }
                else
                {
                    Debug.Log("Failed joining lobby.");
                    OnClientDisconnect(0);
                }
            }
            else
            {
                ConnectClient();
            }
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
