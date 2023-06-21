using System;
using System.Collections;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a client attempting to reconnect to a server. It will try to reconnect a
    /// number of times defined by the ConnectionManager's NbReconnectAttempts property. If it succeeds, it will
    /// transition to the ClientConnected state. If not, it will transition to the Offline state. If given a disconnect
    /// reason first, depending on the reason given, may not try to reconnect again and transition directly to the
    /// Offline state.
    /// </summary>
    class ClientReconnectingState : ClientConnectingState
    {
        [Inject]
        IPublisher<ReconnectMessage> m_ReconnectMessagePublisher;

        Coroutine m_ReconnectCoroutine;
        int m_NbAttempts;

        const float k_TimeBeforeFirstAttempt = 1;
        const float k_TimeBetweenAttempts = 5;

        public override void Enter()
        {
            m_NbAttempts = 0;
            m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
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
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            if (m_NbAttempts < m_ConnectionManager.NbReconnectAttempts)
            {
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
                }
                else
                {
                    var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    m_ConnectStatusPublisher.Publish(connectStatus);
                    switch (connectStatus)
                    {
                        case ConnectStatus.UserRequestedDisconnect:
                        case ConnectStatus.HostEndedSession:
                        case ConnectStatus.ServerFull:
                        case ConnectStatus.IncompatibleBuildType:
                            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                            break;
                        default:
                            m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
                            break;
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    m_ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
                }
                else
                {
                    var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    m_ConnectStatusPublisher.Publish(connectStatus);
                }

                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }

        IEnumerator ReconnectCoroutine()
        {
            // If not on first attempt, wait some time before trying again, so that if the issue causing the disconnect
            // is temporary, it has time to fix itself before we try again. Here we are using a simple fixed cooldown
            // but we could want to use exponential backoff instead, to wait a longer time between each failed attempt.
            // See https://en.wikipedia.org/wiki/Exponential_backoff
            if (m_NbAttempts > 0)
            {
                yield return new WaitForSeconds(k_TimeBetweenAttempts);
            }

            Debug.Log("Lost connection to host, trying to reconnect...");

            m_ConnectionManager.NetworkManager.Shutdown();

            yield return new WaitWhile(() => m_ConnectionManager.NetworkManager.ShutdownInProgress); // wait until NetworkManager completes shutting down
            Debug.Log($"Reconnecting attempt {m_NbAttempts + 1}/{m_ConnectionManager.NbReconnectAttempts}...");
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(m_NbAttempts, m_ConnectionManager.NbReconnectAttempts));

            // If first attempt, wait some time before attempting to reconnect to give time to services to update
            // (i.e. if in a Lobby and the host shuts down unexpectedly, this will give enough time for the lobby to be
            // properly deleted so that we don't reconnect to an empty lobby
            if (m_NbAttempts == 0)
            {
                yield return new WaitForSeconds(k_TimeBeforeFirstAttempt);
            }

            m_NbAttempts++;
            var reconnectingSetupTask = m_ConnectionMethod.SetupClientReconnectionAsync();
            yield return new WaitUntil(() => reconnectingSetupTask.IsCompleted);

            if (!reconnectingSetupTask.IsFaulted && reconnectingSetupTask.Result.success)
            {
                // If this fails, the OnClientDisconnect callback will be invoked by Netcode
                var connectingTask = ConnectClientAsync();
                yield return new WaitUntil(() => connectingTask.IsCompleted);
            }
            else
            {
                if (!reconnectingSetupTask.Result.shouldTryAgain)
                {
                    // setting number of attempts to max so no new attempts are made
                    m_NbAttempts = m_ConnectionManager.NbReconnectAttempts;
                }
                // Calling OnClientDisconnect to mark this attempt as failed and either start a new one or give up
                // and return to the Offline state
                OnClientDisconnect(0);
            }
        }
    }
}
