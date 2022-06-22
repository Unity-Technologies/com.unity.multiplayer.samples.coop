using System;
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
        LobbyServiceFacade m_LobbyServiceFacade;
        ProfileManager m_ProfileManager;

        const string k_MainMenuSceneName = "MainMenu";

        [Inject]
        protected void InjectDependencies(ProfileManager profileManager, LobbyServiceFacade lobbyServiceFacade)
        {
            m_ProfileManager = profileManager;
            m_LobbyServiceFacade = lobbyServiceFacade;
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
            StateChangeRequest?.Invoke(ClientConnecting);
        }

        public override void StartClientLobby(string playerName)
        {
            SetConnectionPayload(GetPlayerId(), playerName);
            StateChangeRequest?.Invoke(ClientConnecting);
        }

        public override void StartHostIP(string playerName, string ipaddress, int port)
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);

            SetConnectionPayload(GetPlayerId(), playerName);
            StateChangeRequest?.Invoke(StartingHost);
        }

        public override void StartHostLobby(string playerName)
        {
            SetConnectionPayload(GetPlayerId(), playerName);
            StateChangeRequest?.Invoke(StartingHost);
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
