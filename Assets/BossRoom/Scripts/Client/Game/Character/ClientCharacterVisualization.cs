using Cinemachine;
using MLAPI;
using System;
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

        private ActionVisualization m_ActionViz;

        private CinemachineVirtualCamera m_MainCamera;

        public Transform Parent { get; private set; }

        public float MinZoomDistance = 3;
        public float MaxZoomDistance = 30;
        public float ZoomSpeed = 3;

        private const float k_MaxVizSpeed = 4;    //max speed at which we will chase the parent transform. 
        private const float k_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second.

        public void Start()
        {
            m_ActionViz = new ActionVisualization(this);
        }

        /// <inheritdoc />
        public override void NetworkStart()
        {
            if (!IsClient || transform.parent == null)
            {
                enabled = false;
                return;
            }

            m_NetState = this.transform.parent.gameObject.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionEventClient += this.PerformActionFX;
            m_NetState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;
            //we want to follow our parent on a spring, which means it can't be directly in the transform hierarchy. 
            Parent = transform.parent;
            Parent.GetComponent<BossRoom.Client.ClientCharacter>().ChildVizObject = this;
            transform.parent = null;


            if (!m_NetState.IsNPC)
            {
                foreach (var model in GetComponents<ModelSwap>())
                {
                    model.SetModel(m_NetState.CharacterAppearance.Value);
                }
            }

            if (IsLocalPlayer)
            {
                AttachCamera();
            }
        }

        private void PerformActionFX(ActionRequestData data)
        {

            m_ActionViz.PlayAction(ref data);
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.ALIVE:
                    m_ClientVisualsAnimator.SetTrigger("StandUp");
                    break;
                case LifeState.FAINTED:
                    m_ClientVisualsAnimator.SetTrigger("FallDown");
                    break;
                case LifeState.DEAD:
                    m_ClientVisualsAnimator.SetTrigger("Dead");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
            }
        }

        void Update()
        {
            if (Parent == null)
            {
                //since we aren't in the transform hierarchy, we have to explicitly die when our parent dies. 
                GameObject.Destroy(this.gameObject);
                return;
            }

            SmoothMove(transform, Parent.transform, Time.deltaTime, k_MaxVizSpeed, k_MaxRotSpeed );

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                m_ClientVisualsAnimator.SetFloat("Speed", m_NetState.NetworkMovementSpeed.Value);
            }

            m_ActionViz.Update();

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0 && m_MainCamera)
            {
                ZoomCamera(scroll);
            }

        }

        public void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured. 

            m_ActionViz.OnAnimEvent(id);
        }

        /// <summary>
        /// Smoothly interpolates towards the parent transform. 
        /// </summary>
        /// <param name="moveTransform">The transform to interpolate</param>
        /// <param name="targetTransform">The transform to interpolate towards.  </param>
        /// <param name="timeDelta">Time in seconds that has elapsed, for purposes of interpolation.</param>
        /// <param name="maxSpeed">The max speed to allow the moveTransform to advance at, in m/s </param>
        /// <param name="maxAngularSpeed">The max angular speed to to rotate at, in degrees/s.</param>
        public static void SmoothMove(Transform moveTransform, Transform targetTransform, float timeDelta, float maxSpeed, float maxAngularSpeed )
        {
            var posDiff = targetTransform.position - moveTransform.position;
            var angleDiff = Quaternion.Angle(targetTransform.transform.rotation, moveTransform.rotation);

            float posDiffMag = posDiff.magnitude;
            if (posDiffMag > 0)
            {
                float maxMove = timeDelta * maxSpeed;
                float moveDist = Mathf.Min(maxMove, posDiffMag);
                posDiff *= (moveDist / posDiffMag);

                moveTransform.position += posDiff;
            }

            if (angleDiff > 0)
            {
                float maxAngleMove = timeDelta * maxAngularSpeed;
                float angleMove = Mathf.Min(maxAngleMove, angleDiff);
                float t = angleMove / angleDiff;
                moveTransform.rotation = Quaternion.Slerp(moveTransform.rotation, targetTransform.rotation, t);
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
