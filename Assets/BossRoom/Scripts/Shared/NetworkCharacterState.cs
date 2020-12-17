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
        public NetworkedVarVector3 NetworkPosition;
        public NetworkedVarFloat NetworkRotationY;

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
