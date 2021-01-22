using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.IO;

namespace BossRoom
{
    /// <summary>
    /// Shared state for a Projectile. 
    /// </summary>
    public class ProjectileNetState : NetworkedBehaviour, INetMovement
    {
        public NetworkedVar<ActionType> SourceAction;

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


        public void ServerBroadcastEnemyHit(ulong enemyId)
        {
            using (Stream stream = PooledBitStream.Get())
            {
                using(PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt64(enemyId);
                }
                InvokeClientRpcOnEveryonePerformance(RecvHitEnemyClient, stream);
            }
        }


        [MLAPI.Messaging.ClientRPC]
        private void RecvHitEnemyClient(ulong clientId, Stream stream)
        { 
            ulong enemyId;
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                enemyId = reader.ReadUInt64();
            }

            HitEnemyEvent?.Invoke(enemyId);
        }
    }
}
