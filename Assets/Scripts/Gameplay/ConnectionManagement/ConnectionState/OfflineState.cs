using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. From this state we can transition to the
    /// Connecting sate, if starting as a client, or the Hosting state, if starting as a host.
    /// </summary>
    public class OfflineState : ConnectionState
    {
        protected LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;
        ProfileManager m_ProfileManager;

        /// <summary>
        /// How many connections we create a Unity relay allocation for
        /// </summary>
        const int k_MaxUnityRelayConnections = 8;

        const string k_MainMenuSceneName = "MainMenu";

        [Inject]
        protected void InjectDependencies(ProfileManager profileManager, LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby)
        {
            m_ProfileManager = profileManager;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
        }

        public override void Enter()
        {
            m_LobbyServiceFacade.EndTracking();
            if (SceneManager.GetActiveScene().name != k_MainMenuSceneName)
            {
                SceneLoaderWrapper.Instance.LoadScene(k_MainMenuSceneName, useNetworkSceneManager: false);
            }
        }

        public override void Exit() { }

        public override void StartClientIP(string playerName, string ipaddress, int port)
        {
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);
            SetConnectionPayload(GetPlayerId(), playerName);
            m_ConnectionManager.ChangeState(ClientConnecting);
        }

        public override async Task StartClientLobbyAsync(string playerName, Action<string> onFailure)
        {
            if (await JoinRelayServerAsync(onFailure))
            {
                SetConnectionPayload(GetPlayerId(), playerName);
                m_ConnectionManager.ChangeState(ClientConnecting);
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

            SetConnectionPayload(GetPlayerId(), playerName);
            m_ConnectionManager.ChangeState(StartingHost);
            return true;
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

            SetConnectionPayload(GetPlayerId(), playerName);
            m_ConnectionManager.ChangeState(StartingHost);
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
