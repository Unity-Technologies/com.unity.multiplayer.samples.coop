using Cinemachine;
using MLAPI;
using UnityEngine;

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

        public Animator OurAnimator { get { return m_ClientVisualsAnimator; } }

        private CinemachineVirtualCamera m_MainCamera;
        private Transform m_Parent;

        public float MinZoomDistance = 3;
        public float MaxZoomDistance = 30;
        public float ZoomSpeed = 3;

        private const float k_MaxVizSpeed = 4;    //max speed at which we will chase the parent transform. 
        private const float x_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second. 

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
            m_Parent.GetComponent<BossRoom.Client.ClientCharacter>().ChildVizObject = this;
            transform.parent = null;

            if (IsLocalPlayer)
            {
                AttachCamera();
            }
        }

        private void PerformActionFX(ActionRequestData data)
        {
            //TODO: [GOMPS-13] break this method out into its own class, so we can drive multi-frame graphical effects. 
            //FIXME: [GOMPS-13] hook this up to information in the ActionDescription. 
            m_ClientVisualsAnimator.SetInteger("AttackID", 1);
            m_ClientVisualsAnimator.SetTrigger("BeginAttack");

            if (data.TargetIds != null && data.TargetIds.Length > 0)
            {
                NetworkedObject targetObject = MLAPI.Spawning.SpawnManager.SpawnedObjects[data.TargetIds[0]];
                if (targetObject != null)
                {
                    var targetAnimator = targetObject.GetComponent<BossRoom.Client.ClientCharacter>().ChildVizObject.OurAnimator;
                    if (targetAnimator != null)
                    {
                        targetAnimator.SetTrigger("BeginHitReact");
                    }
                }
            }
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
            if (scroll != 0 && m_MainCamera)
            {
                ZoomCamera(scroll);
            }

        }

        private void SmoothMove()
        {
            var posDiff = m_Parent.transform.position - transform.position;
            var angleDiff = Quaternion.Angle(m_Parent.transform.rotation, transform.rotation);

            float timeDelta = Time.deltaTime;

            float posDiffMag = posDiff.magnitude;
            if (posDiffMag > 0)
            {
                float maxMove = timeDelta * k_MaxVizSpeed;
                float moveDist = Mathf.Min(maxMove, posDiffMag);
                posDiff *= (moveDist / posDiffMag);

                transform.position += posDiff;
            }

            if (angleDiff > 0)
            {
                float maxAngleMove = timeDelta * x_MaxRotSpeed;
                float angleMove = Mathf.Min(maxAngleMove, angleDiff);
                float t = angleMove / angleDiff;
                transform.rotation = Quaternion.Slerp(transform.rotation, m_Parent.transform.rotation, t);
            }
        }

        private void AttachCamera()
        {
            var cameraGO = GameObject.FindGameObjectWithTag("CMCamera");
            if (cameraGO == null) { return; }

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
