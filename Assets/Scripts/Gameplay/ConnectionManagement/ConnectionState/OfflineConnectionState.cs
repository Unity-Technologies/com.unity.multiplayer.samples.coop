using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class OfflineConnectionState : ConnectionState
    {
        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;

        /// <summary>
        /// How many connections we create a Unity relay allocation for
        /// </summary>
        private const int k_MaxUnityRelayConnections = 8;

        public OfflineConnectionState(ConnectionManager connectionManager, LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby)
            : base(connectionManager)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
        }

        public override void OnClientConnected(ulong clientId)
        {
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            throw new System.NotImplementedException();
        }

        public override void OnServerStarted()
        {
            throw new System.NotImplementedException();
        }

        public override void StartClientIP(string playerId, string playerName, string ipaddress, int port)
        {
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);
            ConnectClient(playerId, playerName);
            m_ConnectionManager.ChangeState(ConnectionStateType.Connecting);
        }

        public override async Task StartClientLobbyAsync(string playerName, string playerId, Action<string> onFailure)
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
                onFailure?.Invoke(e.Message);
                return;//not re-throwing, but still not allowing to connect
            }

            ConnectClient(playerId, playerName);
            m_ConnectionManager.ChangeState(ConnectionStateType.Connecting);
        }

        public override bool StartHostIP(string playerId, string playerName, string ipaddress, int port)
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);

            var success = StartHost(playerId, playerName);
            if (success)
            {
                m_ConnectionManager.ChangeState(ConnectionStateType.Hosting);
            }

            return success;
        }

        public override async Task StartHostLobbyAsync(string playerId, string playerName)
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

            if (StartHost(playerId, playerName))
            {
                m_ConnectionManager.ChangeState(ConnectionStateType.Hosting);
            }
        }

        public override void OnUserRequestedShutdown()
        {
            throw new System.NotImplementedException();
        }

        public override void OnServerShutdown()
        {
            throw new System.NotImplementedException();
        }

        public override void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            if (m_ConnectionManager.NetworkManager.IsHost)
            {
                var payload = System.Text.Encoding.UTF8.GetString(connectionData);
                var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html

                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, m_ConnectionManager.AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));

                // connection approval will create a player object for you
                connectionApprovedCallback(true, null, true, Vector3.zero, Quaternion.identity);
            }
        }

        void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = playerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        void ConnectClient(string playerId, string playerName)
        {
            SetConnectionPayload(playerId, playerName);

            //and...we're off! Netcode will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by getting an ReceiveServerToClientSetDisconnectReason_CustomMessage callback for ourselves and get a message telling us the reason
            //  If the socket connection succeeds, we'll get our ReceiveServerToClientConnectResult_CustomMessage invoked. This is where game-layer failures will be reported.
            m_ConnectionManager.NetworkManager.StartClient();
            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
        }

        bool StartHost(string playerId, string playerName)
        {
            SetConnectionPayload(playerId, playerName);
            return m_ConnectionManager.NetworkManager.StartHost();
        }
    }
}
