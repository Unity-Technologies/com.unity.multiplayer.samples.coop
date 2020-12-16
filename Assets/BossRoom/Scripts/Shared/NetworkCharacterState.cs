using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using UnityEngine;


namespace BossRoom.Shared
{
    // RPCStateComponent from the GDD
    public class NetworkCharacterState : NetworkedBehaviour
    {
        public NetworkedVarVector3 NetworkPosition;
        public NetworkedVarFloat NetworkRotationY;

        // TODO Should we use Unity events or c# events?
        public event Action<Vector3> OnReceivedClientInput;

        [ServerRPC]
        public void ServerRpcReceiveMovementInput(Vector3 position)
        {
            // Assumption that RPC is snaphshotted and buffered already here
            OnReceivedClientInput?.Invoke(position);
        }
    }
}
