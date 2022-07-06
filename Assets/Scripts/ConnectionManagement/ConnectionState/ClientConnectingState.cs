using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state. If
    /// given a disconnect reason first, transitions to the DisconnectingWithReason state.
    /// </summary>
    class ClientConnectingState : ConnectionState
    {
        [Inject]
        protected IPublisher<ConnectStatus> m_ConnectStatusPublisher;
        [Inject]
        protected LobbyServiceFacade m_LobbyServiceFacade;
        [Inject]
        protected LocalLobby m_LocalLobby;

        public override void Enter()
        {
            ConnectClient();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong _)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }

        public override void OnDisconnectReasonReceived(ConnectStatus disconnectReason)
        {
            m_ConnectStatusPublisher.Publish(disconnectReason);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_DisconnectingWithReason);
        }

        protected async Task ConnectClient()
        {
            bool success = true;
            if (m_LobbyServiceFacade.CurrentUnityLobby != null)
            {
                success = await JoinRelayServerAsync();
            }

            if (success)
            {
                success = m_ConnectionManager.NetworkManager.StartClient();
            }

            if (success)
            {
                SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
                m_ConnectionManager.RegisterCustomMessages();
            }
            else
            {
                OnClientDisconnect(0);
            }
        }

        async Task<bool> JoinRelayServerAsync()
        {
            Debug.Log($"Setting Unity Relay client with join code {m_LocalLobby.RelayJoinCode}");

            try
            {
                var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) =
                    await UnityRelayUtilities.JoinRelayServerFromJoinCode(m_LocalLobby.RelayJoinCode);

                await m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(allocationIdBytes.ToString(), m_LocalLobby.RelayJoinCode);
                var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
                utp.SetClientRelayData(ipv4Address, port, allocationIdBytes, key, connectionData, hostConnectionData, isSecure: true);
            }
            catch (Exception e)
            {
                Debug.Log($"Relay join failed: {e.Message}");
                //leave the lobby if relay failed for some reason
                await m_LobbyServiceFacade.EndTracking();
                return false;
            }

            return true;
        }
    }
}
