using BossRoom.Shared;
using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacterVisualization : NetworkedBehaviour
    {
        private NetworkCharacterState networkCharacterState;

        /// <summary>
        /// The GameObject which visually represents the character is a child object of the character GameObject. This needs to be the case to support host mode.
        /// In host mode <see cref="MonoBehaviour.transform"/> is the transform which is relevant for gameplay.
        /// <see cref="m_ClientVisuals"/> is the visual representation on the client side which has interpolated position values.
        /// </summary>
        [SerializeField] private Transform m_ClientVisuals;

        /// <inheritdoc />
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
            m_ClientVisuals.position = networkCharacterState.NetworkPosition.Value;

            m_ClientVisuals.rotation = Quaternion.Euler(0, networkCharacterState.NetworkRotationY.Value, 0);
        }
    }
}