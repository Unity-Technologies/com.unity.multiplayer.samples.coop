using System;
using System.Collections;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a listening host. Handles incoming client connections. When shutting down or
    /// being timed out, transitions to the Offline state.
    /// </summary>
    public class HostingState : ConnectionState
    {
        LobbyServiceFacade m_LobbyServiceFacade;
        IPublisher<ConnectionEventMessage> m_ConnectionEventPublisher;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        const int k_MaxConnectPayload = 1024;

        [Inject]
        void InjectDependencies(LobbyServiceFacade lobbyServiceFacade,
            IPublisher<ConnectionEventMessage> connectionEventPublisher)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_ConnectionEventPublisher = connectionEventPublisher;
        }

        public override void Enter()
        {
            var gameState = UnityEngine.Object.Instantiate(m_ConnectionManager.GameState);

            gameState.Spawn();

            SceneLoaderWrapper.Instance.AddOnSceneEventCallback();

            //The "BossRoom" server always advances to CharSelect immediately on start. Different games
            //may do this differently.
            SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);
        }

        public override void Exit()
        {
            if (m_LobbyServiceFacade.CurrentUnityLobby != null)
            {
                m_LobbyServiceFacade.DeleteLobbyAsync(m_LobbyServiceFacade.CurrentUnityLobby.Id);
            }
            SessionManager<SessionPlayerData>.Instance.OnServerEnded();
        }

        public override void OnClientConnected(ulong clientId)
        {
            m_ConnectionEventPublisher.Publish(new ConnectionEventMessage() { ConnectStatus = ConnectStatus.Success, PlayerName = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId)?.PlayerName });
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                m_ConnectionManager.ChangeState(Offline);
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

        public override void OnUserRequestedShutdown()
        {
            ConnectionManager.SendServerToAllClientsSetDisconnectReason(ConnectStatus.HostEndedSession);
            // Wait before shutting down to make sure clients receive that message before they are disconnected
            m_ConnectionManager.StartCoroutine(WaitToShutdown());
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalCallback" exposed by the NetworkManager, and is run every time a client connects to us.
        /// </summary>
        /// <remarks>
        /// Since our game doesn't have to interact with some third party authentication service to validate the identity of the new connection, our ApprovalCheck
        /// method is simple, and runs synchronously, invoking "callback" to signal approval at the end of the method. Netcode currently doesn't support the ability
        /// to send back more than a "true/false", which means we have to work a little harder to provide a useful error return to the client. To do that, we invoke a
        /// custom message in the same channel that Netcode uses for its connection callback. Since the delivery is NetworkDelivery.ReliableSequenced, we can be
        /// confident that our login result message will execute before any disconnect message.
        /// </remarks>
        /// <param name="connectionData">binary data passed into StartClient. In our case this is defined by the class ConnectionPayload. </param>
        /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
        /// <param name="connectionApprovedCallback">The delegate we must invoke to signal that the connection was approved or not. </param>
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
                    new SessionPlayerData(clientId, connectionPayload.playerName, new NetworkGuid(), 0, true));

                // connection approval will create a player object for you
                connectionApprovedCallback(true, null, true, Vector3.zero, Quaternion.identity);
            }
            else
            {
                //TODO-FIXME:Netcode Issue #796. We should be able to send a reason and disconnect without a coroutine delay.
                //TODO:Netcode: In the future we expect Netcode to allow us to return more information as part of the
                //approval callback, so that we can provide more context on a reject. In the meantime we must provide
                //the extra information ourselves, and then wait a short time before manually close down the connection.

                ConnectionManager.SendServerToClientSetDisconnectReason(clientId, gameReturnStatus);
                m_ConnectionManager.StartCoroutine(WaitToDenyApproval(connectionApprovedCallback));
                if (m_LobbyServiceFacade.CurrentUnityLobby != null)
                {
                    m_LobbyServiceFacade.RemovePlayerFromLobbyAsync(connectionPayload.playerId, m_LobbyServiceFacade.CurrentUnityLobby.Id);
                }
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
            yield return null;
            connectionApprovedCallback(false, 0, false, null, null);
        }

        IEnumerator WaitToShutdown()
        {
            yield return null;
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(Offline);
        }
    }
}
