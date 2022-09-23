using System;
using System.Collections;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
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
        [Inject]
        IPublisher<ReconnectMessage> m_ReconnectMessagePublisher;

        Coroutine m_ReconnectCoroutine;
        string m_LobbyCode = "";
        int m_NbAttempts;

        const float k_TimeBetweenLobbyReconnectAttempts = 1;
        const float k_NbLobbyReconnectAttempts = 5;

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
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(m_ConnectionManager.NbReconnectAttempts, m_ConnectionManager.NbReconnectAttempts));
        }

        public override void OnClientConnected(ulong _)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            if (m_NbAttempts < m_ConnectionManager.NbReconnectAttempts)
            {
                m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
            }
            else
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
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

            m_ConnectionManager.NetworkManager.Shutdown();

            yield return new WaitWhile(() => m_ConnectionManager.NetworkManager.ShutdownInProgress); // wait until NetworkManager completes shutting down
            Debug.Log($"Reconnecting attempt {m_NbAttempts + 1}/{m_ConnectionManager.NbReconnectAttempts}...");
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(m_NbAttempts, m_ConnectionManager.NbReconnectAttempts));
            m_NbAttempts++;
            if (!string.IsNullOrEmpty(m_LobbyCode))
            {
                // Attempting to reconnect to lobby
                var reconnectedToLobby = false;
                var nbLobbyReconnectAttempts = 0;
                while (!reconnectedToLobby && nbLobbyReconnectAttempts < k_NbLobbyReconnectAttempts)
                {
                    nbLobbyReconnectAttempts++;
                    var reconnectingToLobby = m_LobbyServiceFacade.ReconnectToLobbyAsync(m_LocalLobby?.LobbyID);
                    yield return new WaitUntil(() => reconnectingToLobby.IsCompleted);
                    if (!reconnectingToLobby.IsFaulted && reconnectingToLobby.Result != null)
                    {
                        reconnectedToLobby = true;
                    }
                    else
                    {
                        yield return new WaitForSeconds(k_TimeBetweenLobbyReconnectAttempts);
                    }
                }

                if (reconnectedToLobby)
                {
                    // If succeeded, attempt to connect to Relay
                    var connectingToRelay = ConnectClient();
                    yield return new WaitUntil(() => connectingToRelay.IsCompleted);
                }
                else
                {
                    Debug.Log("Failed reconnecting to lobby.");
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
