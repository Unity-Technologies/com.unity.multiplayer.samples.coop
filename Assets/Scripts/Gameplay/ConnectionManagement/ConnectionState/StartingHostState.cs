using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a host starting up. Starts the host when entering the state. If successful,
    /// transitions to the Hosting state, if not, transitions back to the Offline state.
    /// </summary>
    public class StartingHostState : ConnectionState

    {
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        [Inject]
        protected void InjectDependencies(IPublisher<ConnectStatus> connectStatusPublisher)
        {
            m_ConnectStatusPublisher = connectStatusPublisher;
        }

        public override void Enter()
        {
            var success = m_ConnectionManager.NetworkManager.StartHost();
            if (!success)
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
                m_ConnectionManager.ChangeState(Offline);
            }
            else
            {
                m_ConnectionManager.ChangeState(Hosting);
            }
        }

        public override void Exit() { }

        public override void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate connectionApprovedCallback)
        {
            // This happens when starting as a host, before the end of the StartHost call. In that case, we simply approve ourselves.
            if (m_ConnectionManager.NetworkManager.IsHost && clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                var payload = System.Text.Encoding.UTF8.GetString(connectionData);
                var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html

                SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId,
                    new SessionPlayerData(clientId, connectionPayload.playerName, new NetworkGuid(), 0, true));

                // connection approval will create a player object for you
                connectionApprovedCallback(true, null, true, Vector3.zero, Quaternion.identity);
            }
        }
    }
}
