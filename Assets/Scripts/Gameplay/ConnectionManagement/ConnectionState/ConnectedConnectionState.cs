using System;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class ConnectedConnectionState : ConnectionState
    {
        public ConnectedConnectionState(ConnectionManager connectionManager)
            : base(connectionManager)
        {
        }

        public override void OnClientConnected(ulong clientId)
        {
            throw new NotImplementedException();
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            // This is also called on the Host when a different client disconnects. To make sure we only handle our own disconnection, verify that we are either
            // not a host (in which case we know this is about us) or that the clientID is the same as ours if we are the host.
            /*if (!NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsHost && NetworkManager.Singleton.LocalClientId == clientId)
            {
                //On a client disconnect we want to take them back to the main menu.
                //We have to check here in SceneManager if our active scene is the main menu, as if it is, it means we timed out rather than a raw disconnect;
                switch (DisconnectReason.Reason)
                {
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.HostEndedSession:
                    case ConnectStatus.ServerFull:

                        m_QuitGameSessionPub.Publish(new QuitGameSessionMessage() {UserRequested = false}); // go through the normal leave flow
                        break;
                    case ConnectStatus.LoggedInAgain:
                        if (m_TryToReconnectCoroutine == null)
                        {
                            m_QuitGameSessionPub.Publish(new QuitGameSessionMessage() {UserRequested = false}); // if not trying to reconnect, go through the normal leave flow
                        }

                        break;
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.Undefined:
                        // try reconnecting
                        m_ConnectionManager.ChangeState(ConnectionStateType.Reconnecting);
                        break;
                    default:
                        throw new NotImplementedException(DisconnectReason.Reason.ToString());
                }

                m_ConnectStatusPub.Publish(DisconnectReason.Reason);
                DisconnectReason.Clear();
            }*/
        }

        public override void OnServerStarted()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void OnServerShutdown()
        {
            throw new NotImplementedException();
        }

        public override void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            throw new NotImplementedException();
        }
    }
}
