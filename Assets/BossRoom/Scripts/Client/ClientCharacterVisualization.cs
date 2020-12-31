using MLAPI;
using UnityEngine;
using Cinemachine;

namespace BossRoom.Viz
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacterVisualization : NetworkedBehaviour
    {
        private NetworkCharacterState m_NetState;
        private Animator m_ClientVisualsAnimator;
        private CinemachineVirtualCamera m_MainCamera;

        public float MinZoomDistance = 3;
        public float MaxZoomDistance = 30;
        public float ZoomSpeed = 3;

        /// <inheritdoc />
        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
                return;
            }

            m_NetState = this.transform.parent.gameObject.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionEventClient += this.PerformActionFX;

            GetComponent<ModelSwap>();
            
            //GetComponents<ModelSwap>
            
            if (IsLocalPlayer)
            {
                AttachCamera();
            }
        }

        private void PerformActionFX(ActionRequestData data )
        {
            //TODO: [GOMPS-13] break this method out into its own class, so we can drive multi-frame graphical effects. 
            //FIXME: [GOMPS-13] hook this up to information in the ActionDescription. 
            m_ClientVisualsAnimator.SetInteger("AttackID", 1);
            m_ClientVisualsAnimator.SetTrigger("BeginAttack");
        }

        void Awake()
        {
            m_ClientVisualsAnimator = GetComponent<Animator>();
        }

        void Update()
        {
            // TODO Needs core sdk support. This and rotation should grab the interpolated value of network position based on the last received snapshots.
            transform.position = m_NetState.NetworkPosition.Value;

            transform.rotation = Quaternion.Euler(0, m_NetState.NetworkRotationY.Value, 0);

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                m_ClientVisualsAnimator.SetFloat("Speed", m_NetState.NetworkMovementSpeed.Value);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0 && m_MainCamera )
            {
                ZoomCamera(scroll);
            }

        }

        private void AttachCamera()
        {
            m_MainCamera = (CinemachineVirtualCamera)FindObjectOfType(typeof(CinemachineVirtualCamera));
            if (m_MainCamera)
            {
                m_MainCamera.Follow = transform;
                m_MainCamera.LookAt = transform;
            }
        }

        private void ZoomCamera(float scroll)
        {
            CinemachineComponentBase[] components = m_MainCamera.GetComponentPipeline();
            foreach (CinemachineComponentBase component in components)
            {
                if (component is CinemachineFramingTransposer)
                {
                    CinemachineFramingTransposer c = (CinemachineFramingTransposer)component;
                    c.m_CameraDistance += -scroll * ZoomSpeed;
                    if (c.m_CameraDistance < MinZoomDistance)
                        c.m_CameraDistance = MinZoomDistance;
                    if (c.m_CameraDistance > MaxZoomDistance)
                        c.m_CameraDistance = MaxZoomDistance;
                }
            }
        }
    }
}