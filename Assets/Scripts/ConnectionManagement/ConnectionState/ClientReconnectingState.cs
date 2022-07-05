using System;
using System.Collections;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a client attempting to reconnect to a server. It will try to reconnect a
    /// number of times defined by k_NbReconnectAttempts. If it succeeds, it will transition to the ClientConnected
    /// state. If not, it will transition to the Offline state. If given a disconnect reason first, depending on the
    /// reason given, may transition to the DisconnectingWithReason state.
    /// </summary>
    class ClientReconnectingState : ClientConnectingState
    {
        const int k_NbReconnectAttempts = 2;

        [Inject]
        IPublisher<ReconnectMessage> m_ReconnectMessagePublisher;

        Coroutine m_ReconnectCoroutine;
        string m_LobbyCode = "";
        int m_NbAttempts;

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

        public override void OnClientConnected(ulong _)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            if (m_NbAttempts < k_NbReconnectAttempts)
            {
                m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
            }
            else
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }

        public override void OnDisconnectReasonReceived(ConnectStatus disconnectReason)
        {
            m_ConnectStatusPublisher.Publish(disconnectReason);
            switch (disconnectReason)
            {
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.HostEndedSession:
                case ConnectStatus.ServerFull:
                    m_ConnectionManager.ChangeState(m_ConnectionManager.m_DisconnectingWithReason);
                    break;
            }
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
                    var connectingToRelay = ConnectClient();
                    yield return new WaitUntil(() => connectingToRelay.IsCompleted);
                }
                else
                {
                    Debug.Log("Failed joining lobby.");
                    OnClientDisconnect(0);
                }
            }
            else
            {
                var connectingClient = ConnectClient();
                yield return new WaitUntil(() => connectingClient.IsCompleted);
            }
        }
    }
}
