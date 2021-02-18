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

        private const float k_MaxSmoothSpeed = 50;
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

            m_NetState = transform.parent.gameObject.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelActionEventClient += CancelActionFX;
            m_NetState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;
            m_NetState.OnPerformHitReaction += OnPerformHitReaction;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            // With this call, players connecting to a game with down imps will see all of them do the "dying" animation.
            // we should investigate for a way to have the imps already appear as down when connecting.
            // todo gomps-220
            OnLifeStateChanged(m_NetState.NetworkLifeState.Value, m_NetState.NetworkLifeState.Value);

            //we want to follow our parent on a spring, which means it can't be directly in the transform hierarchy.
            Parent = transform.parent;
            Parent.GetComponent<Client.ClientCharacter>().ChildVizObject = this;
            transform.parent = null;


            if (!m_NetState.IsNpc)
            {
                Client.CharacterSwap model = GetComponent<Client.CharacterSwap>();
                model.SwapToModel(m_NetState.CharacterAppearance.Value);
            }

            if (IsLocalPlayer)
            {
                AttachCamera();
            }
        }

        private void OnDestroy()
        {
            if (m_NetState)
            {
                m_NetState.DoActionEventClient -= PerformActionFX;
                m_NetState.CancelActionEventClient -= CancelActionFX;
                m_NetState.NetworkLifeState.OnValueChanged -= OnLifeStateChanged;
                m_NetState.OnPerformHitReaction -= OnPerformHitReaction;
                m_NetState.OnStopChargingUpClient -= OnStoppedChargingUp;
            }
        }

        private void OnPerformHitReaction()
        {
            m_ClientVisualsAnimator.SetTrigger("HitReact1");
        }

        private void PerformActionFX(ActionRequestData data)
        {
            m_ActionViz.PlayAction(ref data);
        }

        private void CancelActionFX()
        {
            m_ActionViz.CancelActions();
        }

        private void OnStoppedChargingUp()
        {
            m_ActionViz.OnStoppedChargingUp();
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.Alive:
                    m_ClientVisualsAnimator.SetTrigger("StandUp");
                    break;
                case LifeState.Fainted:
                    m_ClientVisualsAnimator.SetTrigger("FallDown");
                    break;
                case LifeState.Dead:
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
                // since we aren't in the transform hierarchy, we have to explicitly die when our parent dies.
                Destroy(gameObject);
                return;
            }

            VisualUtils.SmoothMove(transform, Parent.transform, Time.deltaTime,
                k_MaxSmoothSpeed, k_MaxRotSpeed);

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                m_ClientVisualsAnimator.SetFloat("Speed", m_NetState.VisualMovementSpeed.Value);
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
