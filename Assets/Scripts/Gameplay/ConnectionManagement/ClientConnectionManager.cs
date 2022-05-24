using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    enum ConnectionState
    {
        Offline,
        Connecting,
        Connected,
        Reconnecting
    }

    public class ClientConnectionManager : MonoBehaviour
    {
        NetworkManager m_NetworkManager;
        ProfileManager m_ProfileManager;
        ConnectionState m_ConnectionState;
        ConnectStatus m_ConnectStatus;

        /// <summary>
        /// the name of the player chosen at game start
        /// </summary>
        public string PlayerName;

        void Awake()
        {
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }

        void Destroy()
        {
            m_NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        void OnClientDisconnect(ulong clientId)
        {
            throw new NotImplementedException();
        }

        void OnClientConnected(ulong clientId)
        {
            throw new NotImplementedException();
        }

        public void OnUserDisconnectRequest()
        {

        }

        public void StartClientLobby()
        {

        }

        public void StartClientIp()
        {

        }

        void ConnectClient()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = GetPlayerId(),
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = PlayerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_NetworkManager.NetworkConfig.ConnectionData = payloadBytes;

            //and...we're off! Netcode will establish a socket connection to the host.
            //  If the socket connection fails, we'll hear back by getting an ReceiveServerToClientSetDisconnectReason_CustomMessage callback for ourselves and get a message telling us the reason
            //  If the socket connection succeeds, we'll get our ReceiveServerToClientConnectResult_CustomMessage invoked. This is where game-layer failures will be reported.
            m_NetworkManager.StartClient();
            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();

            // should only do this once StartClient has been called (start client will initialize CustomMessagingManager
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientSetDisconnectReason_CustomMessage), ReceiveServerToClientSetDisconnectReason_CustomMessage);
        }

        public void ReceiveServerToClientSetDisconnectReason_CustomMessage(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ConnectStatus status);
            m_ConnectStatus = status;
        }

        public string GetPlayerId()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
        }
    }
}
