using BossRoom.Shared;
using MLAPI;
using UnityEngine;
using Cinemachine;

namespace BossRoom.Client
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacterVisualization : NetworkedBehaviour
    {
        private NetworkCharacterState m_NetworkCharacterState;
        private Animator m_ClientVisualsAnimator;

        /// <summary>
        /// The GameObject which visually represents the character is a child object of the character GameObject. This needs to be the case to support host mode.
        /// In host mode <see cref="MonoBehaviour.transform"/> is the transform which is relevant for gameplay.
        /// <see cref="m_ClientVisuals"/> is the visual representation on the client side which has interpolated position values.
        /// </summary>
        [SerializeField]
        private Transform m_ClientVisuals;

        /// <inheritdoc />
        public override void NetworkStart()
        {
            if (!IsClient && !IsHost)
            {
                enabled = false;
            }
            else if (IsLocalPlayer)
            {
                AttachCamera();
            }
        }

        void Awake()
        {
            m_NetworkCharacterState = GetComponent<NetworkCharacterState>();
            m_ClientVisualsAnimator = m_ClientVisuals.GetComponent<Animator>();
        }

        void Update()
        {
            // TODO Needs core sdk support. This and rotation should grab the interpolated value of network position based on the last received snapshots.
            m_ClientVisuals.position = m_NetworkCharacterState.NetworkPosition.Value;

            m_ClientVisuals.rotation = Quaternion.Euler(0, m_NetworkCharacterState.NetworkRotationY.Value, 0);

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                m_ClientVisualsAnimator.SetFloat("Speed", networkCharacterState.NetworkMovementSpeed.Value);
            }
        }

        private void AttachCamera()
        {
            CinemachineVirtualCamera cam = (CinemachineVirtualCamera)FindObjectOfType(typeof(CinemachineVirtualCamera));
            if (cam)
            {
                cam.Follow = m_ClientVisuals.transform;
                cam.LookAt = m_ClientVisuals.transform;
            }
        }
    }
}