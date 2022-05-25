using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.Utilities;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientCharacterVisualization : MonoBehaviour
    {
        [SerializeField]
        Animator m_ClientVisualsAnimator;

        CharacterSwap m_CharacterSwapper;

        [SerializeField]
        VisualizationConfiguration m_VisualizationConfiguration;

        /// <summary>
        /// Returns a reference to the active Animator for this visualization
        /// </summary>
        public Animator OurAnimator => m_ClientVisualsAnimator;

        /// <summary>
        /// Returns the targeting-reticule prefab for this character visualization
        /// </summary>
        public GameObject TargetReticulePrefab => m_VisualizationConfiguration.TargetReticule;

        /// <summary>
        /// Returns the Material to plug into the reticule when the selected entity is hostile
        /// </summary>
        public Material ReticuleHostileMat => m_VisualizationConfiguration.ReticuleHostileMat;

        /// <summary>
        /// Returns the Material to plug into the reticule when the selected entity is friendly
        /// </summary>
        public Material ReticuleFriendlyMat => m_VisualizationConfiguration.ReticuleFriendlyMat;

        PhysicsWrapper m_PhysicsWrapper;

        public bool CanPerformActions => m_NetState.CanPerformActions;

        NetworkCharacterState m_NetState;
        public NetworkCharacterState NetState => m_NetState;

        ActionVisualization m_ActionViz;

        PositionLerper m_PositionLerper;
        RotationLerper m_RotationLerper;

        // this value suffices for both positional and rotational interpolations; one may have a constant value for each
        const float k_LerpTime = 0.08f;

        Vector3 m_LerpedPosition;
        Quaternion m_LerpedRotation;

        bool m_IsHost;

        float m_CurrentSpeed;

        public NetcodeHooks NetcodeHooks;

        void Awake()
        {
            enabled = false;
            NetcodeHooks = GetComponent<NetcodeHooks>();
            NetcodeHooks.OnNetworkSpawnHook += OnSpawn;
            NetcodeHooks.OnNetworkDespawnHook += OnDespawn;
        }

        void OnSpawn()
        {
            if (!NetworkManager.Singleton.IsClient || transform.parent == null)
            {
                return;
            }

            enabled = true;

            m_IsHost = NetworkManager.Singleton.IsHost;

            m_ActionViz = new ActionVisualization(this);

            m_NetState = GetComponentInParent<NetworkCharacterState>();

            m_PhysicsWrapper = m_NetState.GetComponent<PhysicsWrapper>();

            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelAllActionsEventClient += CancelAllActionFXs;
            m_NetState.CancelActionsByTypeEventClient += CancelActionFXByType;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            m_NetState.IsStealthy.OnValueChanged += OnStealthyChanged;
            m_NetState.MovementStatus.OnValueChanged += OnMovementStatusChanged;
            OnMovementStatusChanged(MovementStatus.Normal,m_NetState.MovementStatus.Value);

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

                if (m_NetState.TryGetComponent(out ClientAvatarGuidHandler clientAvatarGuidHandler))
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

                    if (m_NetState.TryGetComponent(out ClientInputSender inputSender))
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

        void OnDespawn()
        {
            if (m_NetState)
            {
                m_NetState.DoActionEventClient -= PerformActionFX;
                m_NetState.CancelAllActionsEventClient -= CancelAllActionFXs;
                m_NetState.CancelActionsByTypeEventClient -= CancelActionFXByType;
                m_NetState.OnStopChargingUpClient -= OnStoppedChargingUp;
                m_NetState.IsStealthy.OnValueChanged -= OnStealthyChanged;

                if (m_NetState.TryGetComponent(out ClientInputSender sender))
                {
                    sender.ActionInputEvent -= OnActionInput;
                    sender.ClientMoveEvent -= OnMoveInput;
                }
            }

            enabled = false;
        }

        void OnDestroy()
        {
            NetcodeHooks.OnNetworkSpawnHook -= OnSpawn;
            NetcodeHooks.OnNetworkDespawnHook -= OnDespawn;
        }

        void OnActionInput(ActionRequestData data)
        {
            m_ActionViz.AnticipateAction(ref data);
        }

        void OnMoveInput(Vector3 position)
        {
            if (!IsAnimating())
            {
                OurAnimator.SetTrigger(m_VisualizationConfiguration.AnticipateMoveTriggerID);
            }
        }

        void PerformActionFX(ActionRequestData data)
        {
            m_ActionViz.PlayAction(ref data);
        }

        void CancelAllActionFXs()
        {
            m_ActionViz.CancelAllActions();
        }

        void CancelActionFXByType(ActionType actionType)
        {
            m_ActionViz.CancelAllActionsOfType(actionType);
        }

        void OnStoppedChargingUp(float finalChargeUpPercentage)
        {
            m_ActionViz.OnStoppedChargingUp(finalChargeUpPercentage);
        }

        void OnStealthyChanged(bool oldValue, bool newValue)
        {
            SetAppearanceSwap();
        }

        void SetAppearanceSwap()
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
        float GetVisualMovementSpeed(MovementStatus movementStatus)
        {
            if (m_NetState.NetworkLifeState.LifeState.Value != LifeState.Alive)
            {
                return m_VisualizationConfiguration.SpeedDead;
            }

            switch (movementStatus)
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
                    throw new Exception($"Unknown MovementStatus {movementStatus}");
            }
        }

        void OnMovementStatusChanged(MovementStatus previousValue, MovementStatus newValue)
        {
            m_CurrentSpeed = GetVisualMovementSpeed(newValue);
        }

        void Update()
        {
            // On the host, Characters are translated via ServerCharacterMovement's FixedUpdate method. To ensure that
            // the game camera tracks a GameObject moving in the Update loop and therefore eliminate any camera jitter,
            // this graphics GameObject's position is smoothed over time on the host. Clients do not need to perform any
            // positional smoothing since NetworkTransform will interpolate position updates on the root GameObject.
            if (m_IsHost)
            {
                // Note: a cached position (m_LerpedPosition) and rotation (m_LerpedRotation) are created and used as
                // the starting point for each interpolation since the root's position and rotation are modified in
                // FixedUpdate, thus altering this transform (being a child) in the process.
                m_LerpedPosition = m_PositionLerper.LerpPosition(m_LerpedPosition,
                    m_PhysicsWrapper.Transform.position);
                m_LerpedRotation = m_RotationLerper.LerpRotation(m_LerpedRotation,
                    m_PhysicsWrapper.Transform.rotation);
                transform.SetPositionAndRotation(m_LerpedPosition, m_LerpedRotation);
            }

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                OurAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, m_CurrentSpeed);
            }

            m_ActionViz.Update();
        }

        void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured.

            if (enabled) // since this gets triggered by Animator, but this component could be disabled server side, we need to check if that's really the case
            {
                m_ActionViz.OnAnimEvent(id);
            }
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
