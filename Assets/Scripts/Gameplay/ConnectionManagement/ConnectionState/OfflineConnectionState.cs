using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. From this state we can transition to the
    /// Connecting sate, if starting as a client, or the Hosting state, if starting as a host.
    /// </summary>
    public class OfflineConnectionState : ConnectionState
    {
        protected LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;
        ProfileManager m_ProfileManager;

        /// <summary>
        /// How many connections we create a Unity relay allocation for
        /// </summary>
        const int k_MaxUnityRelayConnections = 8;

        [Inject]
        protected void InjectDependencies(ProfileManager profileManager, LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby)
        {
            m_ProfileManager = profileManager;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
        }

        public override void Enter() { }

        public override void Exit() { }

        public override void StartClientIP(string playerName, string ipaddress, int port)
        {
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);
            ConnectClient(GetPlayerId(), playerName);
            m_ConnectionManager.ChangeState(Connecting);
        }

        public override async Task StartClientLobbyAsync(string playerName, Action<string> onFailure)
        {
            if (await JoinRelayServerAsync(onFailure))
            {
                ConnectClient(GetPlayerId(), playerName);
                m_ConnectionManager.ChangeState(Connecting);
            }
        }

        protected async Task<bool> JoinRelayServerAsync(Action<string> onFailure)
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
                return false;
            }

            return true;
        }

        public override bool StartHostIP(string playerName, string ipaddress, int port)
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);

            var success = StartHost(GetPlayerId(), playerName);
            if (success)
            {
                m_ConnectionManager.ChangeState(Hosting);
            }

            return success;
        }

        public override async Task StartHostLobbyAsync(string playerName)
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

            if (StartHost(GetPlayerId(), playerName))
            {
                m_ConnectionManager.ChangeState(Hosting);
            }
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
        }

        bool StartHost(string playerId, string playerName)
        {
            SetConnectionPayload(playerId, playerName);
            return m_ConnectionManager.NetworkManager.StartHost();
        }

        string GetPlayerId()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
        }
    }
}
