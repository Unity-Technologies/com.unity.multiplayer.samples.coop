using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a host starting up. Starts the host when entering the state. If successful,
    /// transitions to the Hosting state, if not, transitions back to the Offline state.
    /// </summary>
    class StartingHostState : ConnectionState
    {
        [Inject]
        LobbyServiceFacade m_LobbyServiceFacade;
        [Inject]
        LocalLobby m_LocalLobby;

        /// <summary>
        /// How many connections we create a Unity relay allocation for
        /// </summary>
        const int k_MaxUnityRelayConnections = 8;

        public override void Enter()
        {
            StartHost();
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }

        public override void OnServerStarted()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Hosting);
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            // This happens when starting as a host, before the end of the StartHost call. In that case, we simply approve ourselves.
            if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                var payload = System.Text.Encoding.UTF8.GetString(connectionData);
                var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html

                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, new NetworkGuid(), 0, true));

                // connection approval will create a player object for you
                response.Approved = true;
                response.CreatePlayerObject = true;
            }
        }

        async void StartHost()
        {
            if (m_LobbyServiceFacade.CurrentUnityLobby != null)
            {
                Debug.Log("Setting up Unity Relay host");

                try
                {
                    var (ipv4Address, port, allocationIdBytes, connectionData, key, joinCode) =
                        await UnityRelayUtilities.AllocateRelayServerAndGetJoinCode(k_MaxUnityRelayConnections);

                    m_LocalLobby.RelayJoinCode = joinCode;
                    //next line enabled lobby and relay services integration
                    await m_LobbyServiceFacade.UpdateLobbyDataAsync(m_LocalLobby.GetDataForUnityServices());
                    await m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(allocationIdBytes.ToString(), joinCode);

                    // we now need to set the RelayCode somewhere :P
                    var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
                    utp.SetHostRelayData(ipv4Address, port, allocationIdBytes, key, connectionData, isSecure: true);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat($"{e.Message}");
                    throw;
                }

                Debug.Log($"Created relay allocation with join code {m_LocalLobby.RelayJoinCode}");
            }

            if (!m_ConnectionManager.NetworkManager.StartHost())
            {
                OnClientDisconnect(m_ConnectionManager.NetworkManager.LocalClientId);
            }
        }
    }
}
