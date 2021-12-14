using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Multiplayer.Samples.BossRoom.Client;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacterVisualization : MonoBehaviour
    {
        [SerializeField]
        private Animator m_ClientVisualsAnimator;

        private CharacterSwap m_CharacterSwapper;

        [SerializeField]
        private VisualizationConfiguration m_VisualizationConfiguration;

        /// <summary>
        /// Returns a reference to the active Animator for this visualization
        /// </summary>
        public Animator OurAnimator { get { return m_ClientVisualsAnimator; } }

        /// <summary>
        /// Returns the targeting-reticule prefab for this character visualization
        /// </summary>
        public GameObject TargetReticulePrefab { get { return m_VisualizationConfiguration.TargetReticule; } }

        /// <summary>
        /// Returns the Material to plug into the reticule when the selected entity is hostile
        /// </summary>
        public Material ReticuleHostileMat { get { return m_VisualizationConfiguration.ReticuleHostileMat; } }

        /// <summary>
        /// Returns the Material to plug into the reticule when the selected entity is friendly
        /// </summary>
        public Material ReticuleFriendlyMat { get { return m_VisualizationConfiguration.ReticuleFriendlyMat; } }

        /// <summary>
        /// Returns our pseudo-Parent, the object that owns the visualization.
        /// (We don't have an actual transform parent because we're on a top-level GameObject.)
        /// </summary>
        public Transform Parent { get; private set; }

        PhysicsWrapper m_PhysicsWrapper;

        public bool CanPerformActions { get { return m_NetState.CanPerformActions; } }

        private NetworkCharacterState m_NetState;

        private ActionVisualization m_ActionViz;

        PositionLerper m_PositionLerper;

        RotationLerper m_RotationLerper;

        // this value suffices for both positional and rotational interpolations; one may have a constant value for each
        const float k_LerpTime = 0.08f;

        public bool IsOwner => m_NetState.IsOwner;

        public ulong NetworkObjectId => m_NetState.NetworkObjectId;

        Vector3 m_LerpedPosition;

        Quaternion m_LerpedRotation;

        public void Start()
        {
            if (!NetworkManager.Singleton.IsClient || transform.parent == null)
            {
                enabled = false;
                return;
            }

            m_ActionViz = new ActionVisualization(this);

            m_NetState = GetComponentInParent<NetworkCharacterState>();

            Parent = m_NetState.transform;

            m_PhysicsWrapper = m_NetState.GetComponent<PhysicsWrapper>();

            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelAllActionsEventClient += CancelAllActionFXs;
            m_NetState.CancelActionsByTypeEventClient += CancelActionFXByType;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            m_NetState.IsStealthy.OnValueChanged += OnStealthyChanged;

            // sync our visualization position & rotation to the most up to date version received from server
            transform.SetPositionAndRotation(m_PhysicsWrapper.Transform.position, m_PhysicsWrapper.Transform.rotation);
            m_LerpedPosition = transform.position;
            m_LerpedRotation = transform.rotation;

            // similarly, initialize start position and rotation for smooth lerping purposes
            m_PositionLerper = new PositionLerper(m_PhysicsWrapper.Transform.position, k_LerpTime);
            m_RotationLerper = new RotationLerper(m_PhysicsWrapper.Transform.rotation, k_LerpTime);

            if (!m_NetState.IsNpc)
            {
                name = "AvatarGraphics" + m_NetState.OwnerClientId;

                if (Parent.TryGetComponent(out ClientAvatarGuidHandler clientAvatarGuidHandler))
                {
                    m_ClientVisualsAnimator = clientAvatarGuidHandler.graphicsAnimator;
                }

                m_CharacterSwapper = GetComponentInChildren<CharacterSwap>();

                // ...and visualize the current char-select value that we know about
                SetAppearanceSwap();

                if (m_NetState.IsOwner)
                {
                    ActionRequestData data = new ActionRequestData { ActionTypeEnum = ActionType.GeneralTarget };
                    m_ActionViz.PlayAction(ref data);
                    gameObject.AddComponent<CameraController>();

                    if (Parent.TryGetComponent(out ClientInputSender inputSender))
                    {
                        // TODO: revisit; anticipated actions would play twice on the host
                        if (!NetworkManager.Singleton.IsServer)
                        {
                            inputSender.ActionInputEvent += OnActionInput;
                        }
                        inputSender.ClientMoveEvent += OnMoveInput;
                    }
                }
            }
        }

        private void OnActionInput(ActionRequestData data)
        {
            m_ActionViz.AnticipateAction(ref data);
        }

        private void OnMoveInput(Vector3 position)
        {
            if (!IsAnimating())
            {
                OurAnimator.SetTrigger(m_VisualizationConfiguration.AnticipateMoveTriggerID);
            }
        }

        private void OnDestroy()
        {
            if (m_NetState)
            {
                m_NetState.DoActionEventClient -= PerformActionFX;
                m_NetState.CancelAllActionsEventClient -= CancelAllActionFXs;
                m_NetState.CancelActionsByTypeEventClient -= CancelActionFXByType;
                m_NetState.OnStopChargingUpClient -= OnStoppedChargingUp;
                m_NetState.IsStealthy.OnValueChanged -= OnStealthyChanged;

                if (Parent != null && Parent.TryGetComponent(out ClientInputSender sender))
                {
                    sender.ActionInputEvent -= OnActionInput;
                    sender.ClientMoveEvent -= OnMoveInput;
                }
            }
        }

        private void PerformActionFX(ActionRequestData data)
        {
            m_ActionViz.PlayAction(ref data);
        }

        private void CancelAllActionFXs()
        {
            m_ActionViz.CancelAllActions();
        }

        private void CancelActionFXByType(ActionType actionType)
        {
            m_ActionViz.CancelAllActionsOfType(actionType);
        }

        private void OnStoppedChargingUp(float finalChargeUpPercentage)
        {
            m_ActionViz.OnStoppedChargingUp(finalChargeUpPercentage);
        }

        private void OnStealthyChanged(bool oldValue, bool newValue)
        {
            SetAppearanceSwap();
        }

        private void SetAppearanceSwap()
        {
            if (m_CharacterSwapper)
            {
                var specialMaterialMode = CharacterSwap.SpecialMaterialMode.None;
                if (m_NetState.IsStealthy.Value)
                {
                    if (m_NetState.IsOwner)
                    {
                        specialMaterialMode = CharacterSwap.SpecialMaterialMode.StealthySelf;
                    }
                    else
                    {
                        specialMaterialMode = CharacterSwap.SpecialMaterialMode.StealthyOther;
                    }
                }

                m_CharacterSwapper.SwapToModel(specialMaterialMode);
            }
        }

        /// <summary>
        /// Returns the value we should set the Animator's "Speed" variable, given current gameplay conditions.
        /// </summary>
        private float GetVisualMovementSpeed()
        {
            Assert.IsNotNull(m_VisualizationConfiguration);
            if (m_NetState.NetworkLifeState.LifeState.Value != LifeState.Alive)
            {
                return m_VisualizationConfiguration.SpeedDead;
            }

            switch (m_NetState.MovementStatus.Value)
            {
                case MovementStatus.Idle:
                    return m_VisualizationConfiguration.SpeedIdle;
                case MovementStatus.Normal:
                    return m_VisualizationConfiguration.SpeedNormal;
                case MovementStatus.Uncontrolled:
                    return m_VisualizationConfiguration.SpeedUncontrolled;
                case MovementStatus.Slowed:
                    return m_VisualizationConfiguration.SpeedSlowed;
                case MovementStatus.Hasted:
                    return m_VisualizationConfiguration.SpeedHasted;
                case MovementStatus.Walking:
                    return m_VisualizationConfiguration.SpeedWalking;
                default:
                    throw new Exception($"Unknown MovementStatus {m_NetState.MovementStatus.Value}");
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

            // On the host, Characters are translated via ServerCharacterMovement's FixedUpdate method. To ensure that
            // the game camera tracks a GameObject moving in the Update loop and therefore eliminate any camera jitter,
            // this graphics GameObject's position is smoothed over time on the host. Clients do not need to perform any
            // positional smoothing since NetworkTransform will interpolate position updates on the root GameObject.
            if (NetworkManager.Singleton.IsHost)
            {
                // Note: a cached position (m_LerpedPosition) and rotation (m_LerpedRotation) are created and used as
                // the starting point for each interpolation since the root's position and rotation are modified every
                // in FixedUpdate, thus altering this transform (being a child) in the process.
                m_LerpedPosition = m_PositionLerper.LerpPosition(m_LerpedPosition,
                    m_PhysicsWrapper.Transform.position);
                m_LerpedRotation = m_RotationLerper.LerpRotation(m_LerpedRotation,
                    m_PhysicsWrapper.Transform.rotation);
                transform.SetPositionAndRotation(m_LerpedPosition, m_LerpedRotation);
            }

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                OurAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, GetVisualMovementSpeed());
            }

            m_ActionViz.Update();
        }

        void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured.

            m_ActionViz.OnAnimEvent(id);
        }

        public bool IsAnimating()
        {
            if (OurAnimator.GetFloat(m_VisualizationConfiguration.SpeedVariableID) > 0.0) { return true; }

            for (int i = 0; i < OurAnimator.layerCount; i++)
            {
                if (OurAnimator.GetCurrentAnimatorStateInfo(i).tagHash != m_VisualizationConfiguration.BaseNodeTagID)
                {
                    //we are in an active node, not the default "nothing" node.
                    return true;
                }
            }

            return false;
        }
    }
}
