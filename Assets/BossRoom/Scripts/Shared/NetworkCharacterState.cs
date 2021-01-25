using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using MLAPI.Serialization.Pooled;
using System;
using System.IO;
using UnityEngine;


namespace BossRoom
{

    public enum LifeState
    {
        Alive,
        Fainted,
        Dead,
    }

    /// <summary>
    /// Contains all NetworkedVars and RPCs of a character. This component is present on both client and server objects.
    /// </summary>
    public class NetworkCharacterState : NetworkedBehaviour
    {
        /// <summary>
        /// The networked position of this Character. This reflects the authoritative position on the server.
        /// </summary>
        public NetworkedVarVector3 NetworkPosition { get; } = new NetworkedVarVector3();

        /// <summary>
        /// The networked rotation of this Character. This reflects the authoritative rotation on the server.
        /// </summary>
        public NetworkedVarFloat NetworkRotationY { get; } = new NetworkedVarFloat();
        public NetworkedVarFloat NetworkMovementSpeed { get; } = new NetworkedVarFloat();

        /// <summary>
        /// </summary>
        [HideInInspector]
        public NetworkedVarInt HitPoints;

        /// <summary>
        /// </summary>
        [HideInInspector]
        public NetworkedVarInt Mana;

        /// <summary>
        /// </summary>
        public NetworkedVar<LifeState> NetworkLifeState { get; } = new NetworkedVar<LifeState>(LifeState.Alive);

        [Tooltip("NPCs should set this value in their prefab. For players, this value is set at runtime.")]
        public NetworkedVar<CharacterTypeEnum> CharacterType;

        /// <summary>
        /// PCs to represent their appearance via a struct of appearance options (so they can mix-and-match different ears, head, face, etc).
        /// </summary>
        public bool IsNpc
        {
            get
            {
                return GameDataSource.Instance.CharacterDataByType[CharacterType.Value].IsNpc;
            }
        }

        /// <summary>
        /// This is an int rather than an enum because it is a "place-marker" for a more complicated system. Ultimately we would like
        /// PCs to represent their appearance via a struct of appearance options (so they can mix-and-match different ears, head, face, etc).
        /// </summary>
        [Tooltip("Value between 0-7. ClientCharacterVisualization will use this to set up the model (for PCs).")]
        public NetworkedVarInt CharacterAppearance;

        /// <summary>
        /// Gets invoked when inputs are received from the client which own this networked character.
        /// </summary>
        public event Action<Vector3> OnReceivedClientInput;

        private void Awake()
        {
            CharacterClass data;
            bool found = GameDataSource.Instance.CharacterDataByType.TryGetValue(CharacterType.Value, out data);
            if (!found)
            {
                throw new Exception($"gameobject {gameObject.name} has charactertype {CharacterType.Value} specified, which isn't in the GameDataSource's list!");
            }
            HitPoints.Value = data.BaseHP;
            Mana.Value = data.BaseMana;
        }

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
        /// <param name="data">Data about which action to play and its associated details. </param>
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
        public void ServerBroadcastAction(ref ActionRequestData data)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                data.Write(stream);
                InvokeClientRpcOnEveryonePerformance(RecvDoActionClient, stream);
            }
        }

        [ClientRPC]
        private void RecvDoActionClient(ulong clientId, Stream stream)
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
