using System;
using System.Threading.Tasks;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.BossRoom.Utils;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client side.
    /// Please override this abstract class to add a new transport or way of connecting.
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        protected ConnectionManager m_ConnectionManager;
        readonly ProfileManager m_ProfileManager;
        protected readonly string m_PlayerName;

        /// <summary>
        /// Setup the host connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupHostConnectionAsync();


        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupClientConnectionAsync();

        /// <summary>
        /// Setup the client for reconnection prior to reconnecting
        /// </summary>
        /// <returns>
        /// success = true if succeeded in setting up reconnection, false if failed.
        /// shouldTryAgain = true if we should try again after failing, false if not.
        /// </returns>
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        public ConnectionMethodBase(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
        {
            m_ConnectionManager = connectionManager;
            m_ProfileManager = profileManager;
            m_PlayerName = playerName;
        }

        protected void SetConnectionPayload(string playerId, string playerName)
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

        /// Using authentication, this makes sure your session is associated with your account and not your device. This means you could reconnect
        /// from a different device for example. A playerId is also a bit more permanent than player prefs. In a browser for example,
        /// player prefs can be cleared as easily as cookies.
        /// The forked flow here is for debug purposes and to make UGS optional in Boss Room. This way you can study the sample without
        /// setting up a UGS account. It's recommended to investigate your own initialization and IsSigned flows to see if you need
        /// those checks on your own and react accordingly. We offer here the option for offline access for debug purposes, but in your own game you
        /// might want to show an error popup and ask your player to connect to the internet.
        protected string GetPlayerId()
        {
            if (Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
        }
    }

    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    class ConnectionMethodIP : ConnectionMethodBase
    {
        string m_Ipaddress;
        ushort m_Port;

        private string CaCertificate =
@"-----BEGIN CERTIFICATE-----
MIIDpzCCAo+gAwIBAgIUeyWOu7GQSV0N3dZNwRr2Pl8HXPAwDQYJKoZIhvcNAQEL
BQAwYjELMAkGA1UEBhMCQ0ExDzANBgNVBAgMBlF1ZWJlYzERMA8GA1UEBwwITW9u
dHJlYWwxGzAZBgNVBAoMElVuaXR5IFRlY2hub2xvZ2llczESMBAGA1UEAwwJMTI3
LjAuMC4xMCAXDTIzMDMxNzE4MDk0MloYDzMwMjIwNzE4MTgwOTQyWjBiMQswCQYD
VQQGEwJDQTEPMA0GA1UECAwGUXVlYmVjMREwDwYDVQQHDAhNb250cmVhbDEbMBkG
A1UECgwSVW5pdHkgVGVjaG5vbG9naWVzMRIwEAYDVQQDDAkxMjcuMC4wLjEwggEi
MA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQD8ikJud244RTt3tCoBluJxocUw
QboPkWU6FHGPiqztACk5ergbd3zvd2//daM7HVGy857vDoZZ9PvPSP29AvD3eO8v
KFoBWfeTzGjXw0L5YXQ3wxq1fhJ1BOI0XLRVPndhLrBLsETz0XUctZASC/EfPiQD
m5gINh7HcQkwza7z7XfX9+A5ttzFHFntoLOFCL67H6iAEGntj4LX/zlZSnE+1F2L
wTYrQAja4XXvJH2GLCYhBiqYbYuaigKfOQvDXCQWCNeyfD/Xh2ugzBXkMOl+ngV6
Ei7qj2sNWWZh49fmTz9DDjh9Jf3gISpcOjabIpE7ZWYuDwhV8YhssK5fnfrdAgMB
AAGjUzBRMB0GA1UdDgQWBBS1loTCqrnX8gnj/TPUzEwIq8lgWzAfBgNVHSMEGDAW
gBS1loTCqrnX8gnj/TPUzEwIq8lgWzAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3
DQEBCwUAA4IBAQCi+A4Fm4HTbL3rjtDM0mXeqjD+XpnxzmsYmSqUXLaDw4OkTQZG
QcBwWaP8GhbhcCgNdd5wNsF8zi8B8KN2ApuefOy9VJCVzq+ZNVde8ib1BtRRogng
U81Kv5Cah2la16wj7Vq1B4SAbKA7mBtagyF4kOU52W93RmYrXZw1LETK5fDCXmUA
/ddDuls7dwbsdoZSGv1UouMe/u/JU5v5M2L9naXj3ajdrPe4MNDSeYc+kMp4qSmd
02falAMqS4vEdqcems1cLzLNjOnbdz0kjRlC1THNsKbyAC7s3icPC7Sp6eByHI0e
M4VrSMAGTfD7PRdBxHYRKRCvstrx1SINX10u
-----END CERTIFICATE-----";

        private string Certificate1 =
@"-----BEGIN CERTIFICATE-----
MIIDTTCCAjUCFHcd5ngQA5+I7m+bm4zGTdYPBQ0sMA0GCSqGSIb3DQEBCwUAMGIx
CzAJBgNVBAYTAkNBMQ8wDQYDVQQIDAZRdWViZWMxETAPBgNVBAcMCE1vbnRyZWFs
MRswGQYDVQQKDBJVbml0eSBUZWNobm9sb2dpZXMxEjAQBgNVBAMMCTEyNy4wLjAu
MTAgFw0yMzAzMTcxODE0MzJaGA8zMDIyMDcxODE4MTQzMlowYjELMAkGA1UEBhMC
Q0ExDzANBgNVBAgMBlF1ZWJlYzERMA8GA1UEBwwITW9udHJlYWwxGzAZBgNVBAoM
ElVuaXR5IFRlY2hub2xvZ2llczESMBAGA1UEAwwJMTI3LjAuMC4xMIIBIjANBgkq
hkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqo5HsOVA9cASv5HIUg3tCLPFCdVdgtL7
tdY6FXqw4b9u84Xe13yY0D84H8Pon+RYR29d0eQuzYJHG54FjWk6xzPzfHh2eLEc
1xTL705+4prLbc+DjVLY2HChDN5nJibF1Hpxn1I6fCFW7iK8Fd1hgMJSYKvovExB
oLdxQSFg9OAe+sqnCl4RykPur3liBcWOHfAkhJYuYaflnghtftCu2jxwlb6viPe+
Ebnn8hXV/vjvkPNJOabKJx0y5LCpG5YfJQVMujsiIVNRPvYUl0DzJkj2qKRsSXT5
3Wci9mH4sLuBh42HEUpHW4/xJGtCVk4GCjSsvz7KU7ONRZFHh8kGKQIDAQABMA0G
CSqGSIb3DQEBCwUAA4IBAQAK6CKtw4E1pssoyP4VmRB0F5CzhrGlvCayWJ0i9iRx
d3569LmdqKYvjm/lv85zrDlFfYyH/b1OIwPyifBM6OjBI7s4CLAIFAzxhHqWsx5N
k9A+Xa+xtHFMpPprTokPPfkeizt52plBjP9X09a9KSq8PLMtaLsQGmcAXV6hmG71
8yHGDARquUPZeAnU+3zvZHXttwn48edbZADhrqNk8yQOz4JO7XBPVNZS/VBxIWe5
8AuVLZx4R6oBkKTrLlajuCMLySyqGqgi/iRbMSlh616+M0TaXChcv+zEm/pG+X4d
4BPMuR+OHHfHAP0ypkhO7SB/sSNo2dXkCJrETp/R00D8
-----END CERTIFICATE-----";

        // this will be required for DTLS and WSS, removed for security purpose, saved locally
        private string PrivateKey1 = "";

        public ConnectionMethodIP(string ip, ushort port, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            m_Ipaddress = ip;
            m_Port = port;
            m_ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            SetConnectionType(utp, false);
            utp.SetConnectionData(m_Ipaddress, m_Port);
            Debug.Log("[Use Encryption]: " + utp.UseEncryption);
            Debug.Log("[Use WebSockets]: " + utp.UseWebSockets);
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            // Nothing to do here
            return (true, true);
        }

        public override async Task SetupHostConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName); // Need to set connection payload for host as well, as host is a client too
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            SetConnectionType(utp, true);
            utp.SetConnectionData(m_Ipaddress, m_Port);
            Debug.Log("[Use Encryption]: " + utp.UseEncryption);
            Debug.Log("[Use WebSockets]: " + utp.UseWebSockets);
        }

        void SetConnectionType(UnityTransport utp, bool isServer)
        {
            switch (ConnectionTypeDropdown.connectionType)
            {
                case "udp":
                    utp.UseEncryption = false;
                    utp.UseWebSockets = false;
                    break;

                case "dtls":
                    utp.UseEncryption = true;
                    utp.UseWebSockets = false;

                    if (isServer)
                    {
                        utp.SetServerSecrets(Certificate1, PrivateKey1);
                    }
                    else
                    {
                        utp.SetClientSecrets("127.0.0.1", CaCertificate);
                    }

                    break;

                case "ws":
                    utp.UseEncryption = false;
                    utp.UseWebSockets = true;
                    break;

                case "wss":
                    utp.UseEncryption = true;
                    utp.UseWebSockets = true;

                    if (isServer)
                    {
                        utp.SetServerSecrets(Certificate1, PrivateKey1);
                    }
                    else
                    {
                        utp.SetClientSecrets("127.0.0.1", CaCertificate);
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// UTP's Relay connection setup using the Lobby integration
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {
        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;

        public ConnectionMethodRelay(LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay client");

            SetConnectionPayload(GetPlayerId(), m_PlayerName);

            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                throw new Exception("Trying to start relay while Lobby isn't set");
            }

            Debug.Log($"Setting Unity Relay client with join code {m_LocalLobby.RelayJoinCode}");

            // Create client joining allocation from join code
            var joinedAllocation = await RelayService.Instance.JoinAllocationAsync(m_LocalLobby.RelayJoinCode);
            Debug.Log($"client: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}, " +
                $"host: {joinedAllocation.HostConnectionData[0]} {joinedAllocation.HostConnectionData[1]}, " +
                $"client: {joinedAllocation.AllocationId}");

            await m_LobbyServiceFacade.UpdatePlayerDataAsync(joinedAllocation.AllocationId.ToString(), m_LocalLobby.RelayJoinCode);

            // Configure UTP with allocation
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Debug.Log("Connection Type: " + ConnectionTypeDropdown.connectionType);

            if (ConnectionTypeDropdown.connectionType == "wss")
            {
                utp.UseWebSockets = true;
                utp.UseEncryption = true;
            }
            else if (ConnectionTypeDropdown.connectionType == "ws")
            {
                utp.UseWebSockets = true;
                utp.UseEncryption = false;
            }
            else if (ConnectionTypeDropdown.connectionType == "dtls")
            {
                utp.UseWebSockets = false;
                utp.UseEncryption = true;
            }
            else if (ConnectionTypeDropdown.connectionType == "udp")
            {
                utp.UseWebSockets = false;
                utp.UseEncryption = false;
            }

            utp.SetRelayServerData(new RelayServerData(joinedAllocation, ConnectionTypeDropdown.connectionType));
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                Debug.Log("Lobby does not exist anymore, stopping reconnection attempts.");
                return (false, false);
            }

            // When using Lobby with Relay, if a user is disconnected from the Relay server, the server will notify the
            // Lobby service and mark the user as disconnected, but will not remove them from the lobby. They then have
            // some time to attempt to reconnect (defined by the "Disconnect removal time" parameter on the dashboard),
            // after which they will be removed from the lobby completely.
            // See https://docs.unity.com/lobby/reconnect-to-lobby.html
            var lobby = await m_LobbyServiceFacade.ReconnectToLobbyAsync();
            var success = lobby != null;
            Debug.Log(success ? "Successfully reconnected to Lobby." : "Failed to reconnect to Lobby.");
            return (success, true); // return a success if reconnecting to lobby returns a lobby
        }

        public override async Task SetupHostConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay host");

            SetConnectionPayload(GetPlayerId(), m_PlayerName); // Need to set connection payload for host as well, as host is a client too

            // Create relay allocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(m_ConnectionManager.MaxConnectedPlayers, region: null);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            Debug.Log($"server: connection data: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}, " +
                $"allocation ID:{hostAllocation.AllocationId}, region:{hostAllocation.Region}");

            m_LocalLobby.RelayJoinCode = joinCode;

            // next line enables lobby and relay services integration
            await m_LobbyServiceFacade.UpdateLobbyDataAndUnlockAsync();
            await m_LobbyServiceFacade.UpdatePlayerDataAsync(hostAllocation.AllocationIdBytes.ToString(), joinCode);

            // Setup UTP with relay connection info
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Debug.Log("Connection Type: " + ConnectionTypeDropdown.connectionType);

            if (ConnectionTypeDropdown.connectionType == "wss")
            {
                utp.UseWebSockets = true;
                utp.UseEncryption = true;
            }
            else if (ConnectionTypeDropdown.connectionType == "ws")
            {
                utp.UseWebSockets = true;
                utp.UseEncryption = false;
            }
            else if (ConnectionTypeDropdown.connectionType == "dtls")
            {
                utp.UseWebSockets = false;
                utp.UseEncryption = true;
            }
            else if (ConnectionTypeDropdown.connectionType == "udp")
            {
                utp.UseWebSockets = false;
                utp.UseEncryption = false;
            }

            utp.SetRelayServerData(new RelayServerData(hostAllocation, ConnectionTypeDropdown.connectionType)); // This is with DTLS enabled for a secure connection
        }
    }
}
