using System;
using System.IO;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using UnityEngine;
using MLAPI.Serialization.Pooled;


namespace BossRoom
{



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
        public void C2S_DoAction(ref ActionRequestData data)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                SerializeAction(ref data, stream);
                InvokeServerRpcPerformance(RecvDoActionServer, stream);
            }
        }

        /// <summary>
        /// Server->Client RPC that broadcasts this action play to all clients. 
        /// </summary>
        /// <param name="data">The data associated with this Action, including what action type it is.</param>
        public void S2C_BroadcastAction(ref ActionRequestData data )
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                SerializeAction(ref data, stream);
                InvokeClientRpcOnEveryonePerformance(RecvDoActionClient, stream);
            }
        }

        private void SerializeAction( ref ActionRequestData data, PooledBitStream stream )
        {
            var Logic = ActionDescriptionList.LIST[data.ActionTypeEnum][0].Logic;

            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteInt16((short)data.ActionTypeEnum);
                if (Logic == ActionLogic.RANGED)
                {
                    writer.WriteVector3(data.Direction);
                }
                if (Logic == ActionLogic.RANGEDTARGETED)
                {
                    writer.WriteIntArray(data.TargetIds);
                }
            }
        }

        [ClientRPC]
        private void RecvDoActionClient(ulong clientId, Stream stream )
        {
            ActionRequestData data = RecvDoAction(clientId, stream);
            DoActionEventClient?.Invoke(data);
        }

        [ServerRPC]
        private void RecvDoActionServer(ulong clientId, Stream stream)
        {
            ActionRequestData data = RecvDoAction(clientId, stream);
            DoActionEventServer?.Invoke(data);
        }

        private ActionRequestData RecvDoAction(ulong clientId, Stream stream )
        {
            ActionRequestData data = new ActionRequestData();

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                data.ActionTypeEnum = (Action)reader.ReadInt16();

                var Logic = ActionDescriptionList.LIST[data.ActionTypeEnum][0].Logic;

                if( Logic == ActionLogic.RANGED )
                {
                    data.Direction = reader.ReadVector3();
                }
                if( Logic == ActionLogic.RANGEDTARGETED )
                {
                    data.TargetIds = reader.ReadIntArray(data.TargetIds);
                }
            }

            return data;
        }


    }
}
