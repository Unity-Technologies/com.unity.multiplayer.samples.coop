using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Serialization.Pooled;
using System.IO;
using MLAPI.Messaging;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Shared state for a Projectile.
    /// </summary>
    public class NetworkProjectileState : NetworkBehaviour, INetMovement
    {
        public NetworkVariable<ActionType> SourceAction;
        public void InitNetworkPositionAndRotationY(Vector3 initPosition, float initRotationY)
        {
            NetworkPosition.Value = initPosition;
            NetworkRotationY.Value = initRotationY;
        }

        public NetworkVariableVector3 NetworkPosition { get; } = new NetworkVariableVector3();

        /// <summary>
        /// This event is raised when the arrow hit an enemy. The argument is the NetworkObjectId of the enemy.
        /// </summary>
        public System.Action<ulong> HitEnemyEvent;

        /// <summary>
        /// The networked rotation of this Character. This reflects the authorative rotation on the server.
        /// </summary>
        public NetworkVariableFloat NetworkRotationY { get; } = new NetworkVariableFloat();
        public NetworkVariableFloat NetworkMovementSpeed { get; } = new NetworkVariableFloat();

        [ClientRpc]
        public void RecvHitEnemyClientRPC(ulong enemyId)
        {
            HitEnemyEvent?.Invoke(enemyId);
        }
    }
}
