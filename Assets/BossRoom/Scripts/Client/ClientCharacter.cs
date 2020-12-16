using BossRoom.Shared;
using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacter: NetworkedBehaviour
    {
        private NetworkCharacterState networkCharacterState;

        [SerializeField]
        private Transform clientInterpolatedObject;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
            }
        }

        void Awake()
        {
            networkCharacterState = GetComponent<NetworkCharacterState>();
        }

        void Update()
        {
            // TODO Needs core sdk support. This and rotation should grab the interpolated value of network position based on the last received snapshots.
            clientInterpolatedObject.position = networkCharacterState.NetworkPosition.Value;

            clientInterpolatedObject.rotation = Quaternion.Euler(0, networkCharacterState.NetworkRotationY.Value, 0);
        }
    }
}
