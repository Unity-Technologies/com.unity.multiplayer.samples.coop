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

        [SerializeField]
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

        private const float k_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second.

        float m_SmoothedSpeed;

        public bool IsOwner => m_NetState.IsOwner;

        public ulong NetworkObjectId => m_NetState.NetworkObjectId;

        public event Action<Animator> animatorSet;

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

            if (Parent.TryGetComponent(out ClientAvatarGuidHandler clientAvatarGuidHandler))
            {
                m_ClientVisualsAnimator = clientAvatarGuidHandler.graphicsAnimator;

                // Netcode for GameObjects (Netcode) does not currently support NetworkAnimator binding at runtime. The
                // following is a temporary workaround. Future refactorings will enable this functionality.
                animatorSet?.Invoke(clientAvatarGuidHandler.graphicsAnimator);
            }

            m_PhysicsWrapper = m_NetState.GetComponent<PhysicsWrapper>();

            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelAllActionsEventClient += CancelAllActionFXs;
            m_NetState.CancelActionsByTypeEventClient += CancelActionFXByType;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            m_NetState.IsStealthy.OnValueChanged += OnStealthyChanged;

            // sync our visualization position & rotation to the most up to date version received from server
            transform.SetPositionAndRotation(m_PhysicsWrapper.Transform.position, m_PhysicsWrapper.Transform.rotation);

            // ...and visualize the current char-select value that we know about
            SetAppearanceSwap();

            if (!m_NetState.IsNpc)
            {
                name = "AvatarGraphics" + m_NetState.OwnerClientId;

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

            // NetworkTransform is interpolated - we can just apply it's position value to our visual object
            transform.position = m_PhysicsWrapper.Transform.position;
            transform.rotation = m_PhysicsWrapper.Transform.rotation;

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                OurAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, GetVisualMovementSpeed());
            }

            m_ActionViz.Update();
        }

        public void OnAnimEvent(string id)
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
