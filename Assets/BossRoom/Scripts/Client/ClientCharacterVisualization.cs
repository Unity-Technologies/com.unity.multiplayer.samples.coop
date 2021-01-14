using MLAPI;
using UnityEngine;
using Cinemachine;

namespace BossRoom.Visual
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacterVisualization : NetworkedBehaviour
    {
        private NetworkCharacterState m_NetState;

        [SerializeField]
        private Animator m_ClientVisualsAnimator;

        private CinemachineVirtualCamera m_MainCamera;
        private Transform m_Parent;

        public float MinZoomDistance = 3;
        public float MaxZoomDistance = 30;
        public float ZoomSpeed = 3;

        private const float MAX_VIZ_SPEED = 4;    //max speed at which we will chase the parent transform. 
        private const float MAX_ROT_SPEED = 280;  //max angular speed at which we will rotate, in degrees/second. 

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

            //we want to follow our parent on a spring, which means it can't be directly in the transform hierarchy. 
            m_Parent = transform.parent;
            transform.parent = null;

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

        void Update()
        {
            if (m_Parent == null)
            {
                //since we aren't in the transform hierarchy, we have to explicitly die when our parent dies. 
                GameObject.Destroy(this.gameObject);
                return;
            }

            SmoothMove();

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

        private void SmoothMove()
        {
            var pos_diff = m_Parent.transform.position - transform.position;
            var angle_diff = Quaternion.Angle(m_Parent.transform.rotation, transform.rotation);

            float time_delta = Time.deltaTime;

            float pos_diff_mag = pos_diff.magnitude;
            if( pos_diff_mag > 0 )
            {
                float max_move = time_delta * MAX_VIZ_SPEED;
                float move_dist = Mathf.Min(max_move, pos_diff_mag);
                pos_diff *= (move_dist / pos_diff_mag);

                transform.position += pos_diff;
            }

            if( angle_diff > 0 )
            {
                float max_angle_move = time_delta * MAX_ROT_SPEED;
                float angle_move = Mathf.Min(max_angle_move, angle_diff);
                float t = angle_move / angle_diff;
                transform.rotation = Quaternion.Slerp(transform.rotation, m_Parent.transform.rotation, t);
            }
        }

        private void AttachCamera()
        {
            var cameraGO = GameObject.FindGameObjectWithTag("CMCamera");
            if( cameraGO == null ) { return; }

            m_MainCamera = cameraGO.GetComponent<CinemachineVirtualCamera>();
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