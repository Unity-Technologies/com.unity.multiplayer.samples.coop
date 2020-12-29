using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using UnityEngine;


namespace BossRoom.Shared
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
        public NetworkedVarFloat NetworkMovementSpeed;

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
    }
}
