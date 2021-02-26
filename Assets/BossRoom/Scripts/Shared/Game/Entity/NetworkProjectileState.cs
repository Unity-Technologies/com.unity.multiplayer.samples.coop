using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.Serialization.Pooled;
using System.IO;
using MLAPI.Messaging;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Shared state for a Projectile.
    /// </summary>
    public class NetworkProjectileState : NetworkedBehaviour, INetMovement
    {
        public NetworkedVar<ActionType> SourceAction;
        public void InitNetworkPositionAndRotationY(Vector3 initPosition, float initRotationY)
        {
            NetworkPosition.Value = initPosition;
            NetworkRotationY.Value = initRotationY;
        }

        public NetworkedVarVector3 NetworkPosition { get; } = new NetworkedVarVector3();

        /// <summary>
        /// This event is raised when the arrow hit an enemy. The argument is the networkId of the enemy.
        /// </summary>
        public System.Action<ulong> HitEnemyEvent;

        /// <summary>
        /// The networked rotation of this Character. This reflects the authorative rotation on the server.
        /// </summary>
        public NetworkedVarFloat NetworkRotationY { get; } = new NetworkedVarFloat();
        public NetworkedVarFloat NetworkMovementSpeed { get; } = new NetworkedVarFloat();

        [ClientRpc]
        public void RecvHitEnemyClientRPC(ulong enemyId)
        {
            HitEnemyEvent?.Invoke(enemyId);
        }
    }
}
