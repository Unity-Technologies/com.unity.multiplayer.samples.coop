using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class HostingConnectionState : ConnectionState
    {
        LobbyServiceFacade m_LobbyServiceFacade;
        IPublisher<ConnectionEventMessage> m_ConnectionEventPublisher;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        const int k_MaxConnectPayload = 1024;

        public HostingConnectionState(ConnectionManager connectionManager, LobbyServiceFacade lobbyServiceFacade, IPublisher<ConnectionEventMessage> connectionEventPublisher)
            : base(connectionManager)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_ConnectionEventPublisher = connectionEventPublisher;
        }

        public override void OnClientConnected(ulong clientId)
        {
            m_ConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.Success, PlayerName = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId)?.PlayerName });
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                {
                    m_LobbyServiceFacade.DeleteLobbyAsync(m_LobbyServiceFacade.CurrentUnityLobby.Id);
                }
                SessionManager<SessionPlayerData>.Instance.OnServerEnded();
            }
            else
            {
                var playerId = SessionManager<SessionPlayerData>.Instance.GetPlayerId(clientId);
                if (playerId != null)
                {
                    if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                    {
                        m_LobbyServiceFacade.RemovePlayerFromLobbyAsync(playerId, m_LobbyServiceFacade.CurrentUnityLobby.Id);
                    }

                    var sessionData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(playerId);
                    if (sessionData.HasValue)
                    {
                        m_ConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.GenericDisconnect, PlayerName = sessionData.Value.PlayerName });
                    }
                    SessionManager<SessionPlayerData>.Instance.DisconnectClient(clientId);
                }
            }
        }

        public override void OnServerStarted()
        {

        }

        public override void StartClientIP(string playerId, string playerName, string ipaddress, int port)
        {
            throw new NotImplementedException();
        }

        public override Task StartClientLobbyAsync(string playerName, string playerId, Action<string> onFailure)
        {
            throw new NotImplementedException();
        }

        public override bool StartHostIP(string playerId, string playerName, string ipaddress, int port)
        {
            throw new NotImplementedException();
        }

        public override Task StartHostLobbyAsync(string playerId, string playerName)
        {
            throw new NotImplementedException();
        }

        public override void OnUserRequestedShutdown()
        {
            ConnectionManager.SendServerToAllClientsSetDisconnectReason(ConnectStatus.HostEndedSession);
            // Wait before shutting down to make sure clients receive that message before they are disconnected
            //StartCoroutine(WaitToShutdown());
            m_ConnectionManager.NetworkManager.Shutdown();
            SessionManager<SessionPlayerData>.Instance.OnServerEnded();
        }

        public override void OnServerShutdown()
        {
            throw new NotImplementedException();
        }

        public override void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData too high, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                connectionApprovedCallback(false, 0, false, null, null);
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            var gameReturnStatus = GetConnectStatus(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, m_ConnectionManager.AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid(), 0, true));

                // connection approval will create a player object for you
                connectionApprovedCallback(true, null, true, Vector3.zero, Quaternion.identity);
            }
            else
            {
                //TODO-FIXME:Netcode Issue #796. We should be able to send a reason and disconnect without a coroutine delay.
                //TODO:Netcode: In the future we expect Netcode to allow us to return more information as part of the
                //approval callback, so that we can provide more context on a reject. In the meantime we must provide
                //the extra information ourselves, and then wait a short time before manually close down the connection.

                //ConnectionManager.SendServerToClientSetDisconnectReason(clientId, gameReturnStatus);
                //StartCoroutine(WaitToDenyApproval(connectionApprovedCallback));
                if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                {
                    m_LobbyServiceFacade.RemovePlayerFromLobbyAsync(connectionPayload.playerId, m_LobbyServiceFacade.CurrentUnityLobby.Id);
                }
                connectionApprovedCallback(false, 0, false, null, null);
            }
        }

        ConnectStatus GetConnectStatus(ConnectionPayload connectionPayload)
        {
            if (m_ConnectionManager.NetworkManager.ConnectedClientsIds.Count >= CharSelectData.k_MaxLobbyPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.isDebug != Debug.isDebugBuild)
            {
                return ConnectStatus.IncompatibleBuildType;
            }

            return SessionManager<SessionPlayerData>.Instance.IsDuplicateConnection(connectionPayload.playerId) ?
                ConnectStatus.LoggedInAgain : ConnectStatus.Success;
        }

        static IEnumerator WaitToDenyApproval(NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            yield return new WaitForSeconds(0.5f);
            connectionApprovedCallback(false, 0, false, null, null);
        }

        IEnumerator WaitToShutdown()
        {
            yield return null;
            m_ConnectionManager.NetworkManager.Shutdown();
            SessionManager<SessionPlayerData>.Instance.OnServerEnded();
        }
    }
}
