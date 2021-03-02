using BossRoom.Client;
using Cinemachine;
using MLAPI;
using System;
using System.ComponentModel;
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

        [SerializeField]
        private CharacterSwap m_CharacterSwapper;

        [Tooltip("Prefab for the Target Reticule used by this Character")]
        public GameObject TargetReticule;

        [Tooltip("Material to use when displaying a friendly target reticule (e.g. green color)")]
        public Material ReticuleFriendlyMat;

        [Tooltip("Material to use when displaying a hostile target reticule (e.g. red color)")]
        public Material ReticuleHostileMat;

        public Animator OurAnimator { get { return m_ClientVisualsAnimator; } }

        private ActionVisualization m_ActionViz;

        private CinemachineVirtualCamera m_MainCamera;

        public Transform Parent { get; private set; }

        public float MinZoomDistance = 3;
        public float MaxZoomDistance = 30;
        public float ZoomSpeed = 3;

        private const float k_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second.

        /// Player characters need to report health changes and chracter info to the PartyHUD
        private Visual.PartyHUD m_PartyHUD;

        private float m_SmoothedSpeed;

        int m_AliveStateTriggerID;
        int m_FaintedStateTriggerID;
        int m_DeadStateTriggerID;
        int m_HitStateTriggerID;

        /// <inheritdoc />
        public override void NetworkStart()
        {
            if (!IsClient || transform.parent == null)
            {
                enabled = false;
                return;
            }

            m_AliveStateTriggerID = Animator.StringToHash("StandUp");
            m_FaintedStateTriggerID = Animator.StringToHash("FallDown");
            m_DeadStateTriggerID = Animator.StringToHash("Dead");
            m_HitStateTriggerID = Animator.StringToHash("HitReact1");

            m_ActionViz = new ActionVisualization(this);

            Parent = transform.parent;

            m_NetState = Parent.gameObject.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelActionEventClient += CancelActionFX;
            m_NetState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;
            m_NetState.OnPerformHitReaction += OnPerformHitReaction;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;

            //we want to follow our parent on a spring, which means it can't be directly in the transform hierarchy.
            Parent.GetComponent<ClientCharacter>().ChildVizObject = this;
            transform.SetParent(null);

            // sync our visualization position & rotation to the most up to date version received from server
            var parentMovement = Parent.GetComponent<INetMovement>();
            transform.position = parentMovement.NetworkPosition.Value;
            transform.rotation = Quaternion.Euler(0, parentMovement.NetworkRotationY.Value, 0);

            // sync our animator to the most up to date version received from server
            SyncEntryAnimation(m_NetState.NetworkLifeState.Value);

            // listen for char-select info to change (in practice, this info doesn't
            // change, but we may not have the values set yet) ...
            m_NetState.CharacterAppearance.OnValueChanged += OnCharacterAppearanceChanged;

            // ...and visualize the current char-select value that we know about
            OnCharacterAppearanceChanged(0, m_NetState.CharacterAppearance.Value);

            // ...and visualize the current char-select value that we know about
            if (m_CharacterSwapper)
            {
                m_CharacterSwapper.SwapToModel(m_NetState.CharacterAppearance.Value);
            }

            if (!m_NetState.IsNpc)
            {
                // track health for heroes
                m_NetState.HealthState.HitPoints.OnValueChanged += OnHealthChanged;

                var model = GetComponent<CharacterSwap>();
                int heroAppearance = m_NetState.CharacterAppearance.Value;
                model.SwapToModel(heroAppearance);

                // find the emote bar to track its buttons
                GameObject partyHUDobj = GameObject.FindGameObjectWithTag("PartyHUD");
                m_PartyHUD = partyHUDobj.GetComponent<Visual.PartyHUD>();

                if (IsLocalPlayer)
                {
                    ActionRequestData data = new ActionRequestData { ActionTypeEnum = ActionType.GeneralTarget };
                    m_ActionViz.PlayAction(ref data);
                    AttachCamera();
                    m_PartyHUD.SetHeroData(m_NetState);
                }
                else
                {
                    m_PartyHUD.SetAllyType(m_NetState.NetworkId, m_NetState.CharacterType);
                }

            }
        }

        /// <summary>
        /// The switch to certain LifeStates fires an animation on an NPC/PC. This bypasses that initial animation
        /// and sends an NPC/PC to their eventual looping animation. This is necessary for mid-game player connections.
        /// </summary>
        /// <param name="lifeState"> The last LifeState received by server. </param>
        void SyncEntryAnimation(LifeState lifeState)
        {
            switch (lifeState)
            {
                case LifeState.Dead: // ie. NPCs already dead
                    m_ClientVisualsAnimator.SetTrigger(Animator.StringToHash("EntryDeath"));
                    break;
                case LifeState.Fainted: // ie. PCs already fainted
                    m_ClientVisualsAnimator.SetTrigger(Animator.StringToHash("EntryFainted"));
                    break;
            }
        }

        private void OnPerformHitReaction()
        {
            m_ClientVisualsAnimator.SetTrigger(m_HitStateTriggerID);
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
                    m_ClientVisualsAnimator.SetTrigger(m_AliveStateTriggerID);
                    break;
                case LifeState.Fainted:
                    m_ClientVisualsAnimator.SetTrigger(m_FaintedStateTriggerID);
                    break;
                case LifeState.Dead:
                    m_ClientVisualsAnimator.SetTrigger(m_DeadStateTriggerID);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
            }
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            // don't do anything if party HUD goes away - can happen as Dungeon scene is destroyed
            if (m_PartyHUD == null) { return; }

            if (IsLocalPlayer)
            {
                this.m_PartyHUD.SetHeroHealth(newValue);
            }
            else
            {
                this.m_PartyHUD.SetAllyHealth(m_NetState.NetworkId, newValue);
            }
        }

        private void OnCharacterAppearanceChanged(int oldValue, int newValue)
        {
            if (m_CharacterSwapper)
            {
                m_CharacterSwapper.SwapToModel(m_NetState.CharacterAppearance.Value);
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

            VisualUtils.SmoothMove(transform, Parent.transform, Time.deltaTime, ref m_SmoothedSpeed, k_MaxRotSpeed);

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
