using System;
using System.IO;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using UnityEngine;
using MLAPI.Serialization.Pooled;


namespace BossRoom
{

    public enum LifeState
    {
        ALIVE,
        FAINTED,
        DEAD
    }

    /// <summary>
    /// Contains all NetworkedVars and RPCs of a character. This component is present on both client and server objects.
    /// </summary>
    public class NetworkCharacterState : NetworkedBehaviour
    {
        /// <summary>
        /// The networked position of this Character. This reflects the authorative position on the server.
        /// </summary>
        public NetworkedVarVector3 NetworkPosition { get;} = new NetworkedVarVector3();

        /// <summary>
        /// The networked rotation of this Character. This reflects the authorative rotation on the server.
        /// </summary>
        public NetworkedVarFloat NetworkRotationY { get; } = new NetworkedVarFloat();
        public NetworkedVarFloat NetworkMovementSpeed { get; } = new NetworkedVarFloat();

        public NetworkedVarInt HitPoints;
        public NetworkedVarInt Mana; 
        public NetworkedVar<LifeState> NetworkLifeState { get; } = new NetworkedVar<LifeState>(LifeState.ALIVE);
        
        /// <summary>
        /// Gets invoked when inputs are received from the client which own this networked character.
        /// </summary>
        public event Action<Vector3> OnReceivedClientInput;

        /// <summary>
        /// RPC to send inputs for this character from a client to a server.
        /// </summary>
        /// <param name="movementTarget">The position which this character should move towards.</param>
        [ServerRPC]
        public void SendCharacterInputServerRpc(Vector3 movementTarget)
        {
            OnReceivedClientInput?.Invoke(movementTarget);
        }


        // ACTION SYSTEM

        /// <summary>
        /// This event is raised on the server when an action request arrives
        /// </summary>
        public event Action<BossRoom.ActionRequestData> DoActionEventServer;

        /// <summary>
        /// This event is raised on the client when an action is being played back. 
        /// </summary>
        public event Action<BossRoom.ActionRequestData> DoActionEventClient;

        /// <summary>
        /// Client->Server RPC that sends a request to play an action. 
        /// </summary>
        /// <param name="data">Data about which action to play an dits associated details. </param>
        public void ClientSendActionRequest(ref ActionRequestData data)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                data.Write(stream);
                InvokeServerRpcPerformance(RecvDoActionServer, stream);
            }
        }

        /// <summary>
        /// Server->Client RPC that broadcasts this action play to all clients. 
        /// </summary>
        /// <param name="data">The data associated with this Action, including what action type it is.</param>
        public void ServerBroadcastAction(ref ActionRequestData data )
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                data.Write(stream);
                InvokeClientRpcOnEveryonePerformance(RecvDoActionClient, stream);
            }
        }

        [ClientRPC]
        private void RecvDoActionClient(ulong clientId, Stream stream )
        {
            var data = new ActionRequestData();
            data.Read(stream);
            DoActionEventClient?.Invoke(data);
        }

        [ServerRPC]
        private void RecvDoActionServer(ulong clientId, Stream stream)
        {
            var data = new ActionRequestData();
            data.Read(stream);
            DoActionEventServer?.Invoke(data);
        }
    }
}
