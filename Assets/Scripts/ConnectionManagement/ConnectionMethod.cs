using System.Threading.Tasks;
using Unity.BossRoom.UnityServices.Sessions;
using Unity.BossRoom.Utils;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client
    /// side.
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
        public abstract void SetupHostConnection();

        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract void SetupClientConnection();

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
            var payload = JsonUtility.ToJson(new ConnectionPayload
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

        public override void SetupClientConnection()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(m_Ipaddress, m_Port);
            Connection.SetConnectionType(utp, false);
            Debug.Log("[IP Client] Connection type: " + Connection.GetConnectionTypeDescription(utp));
        }

        public override Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            // Nothing to do here
            return Task.FromResult((true, true));
        }

        public override void SetupHostConnection()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName); // Need to set connection payload for host as well, as host is a client too
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(m_Ipaddress, m_Port);
            Connection.SetConnectionType(utp, true);
            Debug.Log("[IP Host] Connection type: " + Connection.GetConnectionTypeDescription(utp));
        }
    }

    /// <summary>
    /// UTP's Relay connection setup using the Session integration
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {
        MultiplayerServicesFacade m_MultiplayerServicesFacade;

        public ConnectionMethodRelay(MultiplayerServicesFacade multiplayerServicesFacade,
            ConnectionManager connectionManager,
            ProfileManager profileManager,
            string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            m_MultiplayerServicesFacade = multiplayerServicesFacade;
            m_ConnectionManager = connectionManager;
        }

        public override void SetupClientConnection()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Connection.SetConnectionType(utp, false);
            Debug.Log("[Relay Client] Connection type: " + Connection.GetConnectionTypeDescription(utp));
        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            if (m_MultiplayerServicesFacade.CurrentUnitySession == null)
            {
                Debug.Log("Session does not exist anymore, stopping reconnection attempts.");
                return (false, false);
            }

            // When using Session with Relay, if a user is disconnected from the Relay server, the server will notify the
            // Session service and mark the user as disconnected, but will not remove them from the Session. They then have
            // some time to attempt to reconnect (defined by the "Disconnect removal time" parameter on the dashboard),
            // after which they will be removed from the Session completely.
            // See https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/join-session#Reconnect_to_a_session
            var session = await m_MultiplayerServicesFacade.ReconnectToSessionAsync();
            var success = session != null;
            Debug.Log(success ? "Successfully reconnected to Session." : "Failed to reconnect to Session.");
            return (success, true); // return a success if reconnecting to session returns a session
        }

        public override void SetupHostConnection()
        {
            Debug.Log("Setting up Unity Relay host");
            
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Connection.SetConnectionType(utp, false);

            SetConnectionPayload(GetPlayerId(), m_PlayerName); // Need to set connection payload for host as well, as host is a client too
            utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Debug.Log("[Relay Host] Connection type: " + Connection.GetConnectionTypeDescription(utp));
        }
    }

    static class Connection
    {
        static private string CaCertificate =
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
        
        static private string Certificate1 =
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
        static private string PrivateKey1 =
            @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAqo5HsOVA9cASv5HIUg3tCLPFCdVdgtL7tdY6FXqw4b9u84Xe
13yY0D84H8Pon+RYR29d0eQuzYJHG54FjWk6xzPzfHh2eLEc1xTL705+4prLbc+D
jVLY2HChDN5nJibF1Hpxn1I6fCFW7iK8Fd1hgMJSYKvovExBoLdxQSFg9OAe+sqn
Cl4RykPur3liBcWOHfAkhJYuYaflnghtftCu2jxwlb6viPe+Ebnn8hXV/vjvkPNJ
OabKJx0y5LCpG5YfJQVMujsiIVNRPvYUl0DzJkj2qKRsSXT53Wci9mH4sLuBh42H
EUpHW4/xJGtCVk4GCjSsvz7KU7ONRZFHh8kGKQIDAQABAoIBAFADCXziP/RKFEGM
yZY89DtF4qT3VLQf+HfYtU0ITtzI6usRnXYn/KSNU+4LASGphQSWKURjMNW2TVeW
bXJmsG1tYMe+141NQYNhPT7Z60qxZcfxNvzgpdv8EoGwAGH0hJBmlNEyST+cgGSd
JPF49tnpz62MWFWlzI/yHw538z1M9wzI1FrgpWkEPnPp24qNvO3sCMNlCkSbOLCi
Nw75Cjt9hVufrsfE3fGfXuJUadsxYxi+H6X57qHEPRebe320Sb/aLSx7IZdxnYwh
ksCXtqlpVjrsDXBiy+Bv2hmBruLtDxAFRwF/Ds+M43/sTcR5RobJjNlmPS2RfQZi
71RknAECgYEA0h4bKHqov7gUrMY65sOCaBTdeKwWgePSETIe6F/P+VoYg/6u+Nex
nYaeXyzQJCv2AUvGscmoJXSrS1v8iSLbFDj0H7DbZdiygVm+XYu7+6KuFWyPdGHO
eenPqjAOVbjxlNzIUmSwyPsmeG+hwrjBuVxheOuRwQAAKCqmhsAEaKkCgYEAz8yb
oma7uKgy4CfwwfKoeG33VFi++Z+WSY+M9u1K2vLwzOXjeKXFAnSZJx6FqoALKc3T
6zXLqcD3sDr0KeMRGAbnee9q5HihCXehfP2o/mYrrn+zFA/ITa6iJnjXcVYplH8a
YL2AgMtpJ5lJP0LlflzwsJeoSRXDJNM+CaKOoYECgYA8deFGspTgJe39EUVdpaBe
prJbyNjpI08NF6kBIKDNlXk8cgqTHC3FsDjeFh0Ga4fsM4vHGMnDjWDE3IE8TMVR
eln0zIU1NzeWNOasMEs1S0hgbc6RpJsvRXI1/IIDdKY/OZCC9OpRysL1INohF9zW
o5iAnPhh2sgwxqUIXTRnkQKBgGr1MSKtaHCKuu0gAc+CnG6og4b5ywrnts6UQgWT
bFU4ePOuXKBsCvTRmUdGcZyqHZTd6feGkBcHSTZ/kc/BnbnVS+46arXA1XrvBcM1
OXgAlPlTp5Rq7zn06meNCa+/ntVdevFSeUNR8AU+5DHYuBGLuyPaW/eKaRCaXrNM
5ceBAoGBAICeGgiHw6ZfiFY6E11XIiFp2Mql7AglOsf0EecdMuf6v0f5I6+LAXKb
5aScXGSAx1hKmADkfbllPpKuefKgJswmmCvHVLWWZLxdHZl/PjZ5rrChEt08Mp81
WUYYLllnPg9Pry+dLnAj73wjbIrAV6mlKl8x94yk8nC0DNK0n4cB
-----END RSA PRIVATE KEY-----";
        
        public static void SetConnectionType(UnityTransport utp, bool isServer)
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
        
        public static string GetConnectionTypeDescription(UnityTransport utp)
        {
            if (utp == null) return "Unknown";
            if (utp.UseWebSockets)
                return utp.UseEncryption ? "WSS" : "WS";
            return utp.UseEncryption ? "DTLS" : "UDP";
        }
    }
}
