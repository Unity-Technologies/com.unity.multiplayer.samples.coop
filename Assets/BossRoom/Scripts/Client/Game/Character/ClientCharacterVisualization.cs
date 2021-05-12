using BossRoom.Client;
using MLAPI;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Visual
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacterVisualization : NetworkBehaviour
    {
        [SerializeField]
        BossRoomPlayerCharacter m_BossRoomPlayerCharacter;

        [SerializeField]
        Animator m_ClientVisualsAnimator;

        [SerializeField]
        CharacterSwap m_CharacterSwapper;

        [SerializeField]
        VisualizationConfiguration m_VisualizationConfiguration;

        [SerializeField]
        TransformVariable m_RuntimeObjectsParent;

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

        /// <summary>
        /// Returns our pseudo-Parent, the object that owns the visualization.
        /// (We don't have an actual transform parent because we're on a top-level GameObject.)
        /// </summary>
        public Transform Parent { get; private set; }

        public bool CanPerformActions => m_NetState.CanPerformActions;

        NetworkCharacterState m_NetState;

        NetworkAppearanceState m_NetworkAppearanceState;

        NetworkLifeState m_NetworkLifeState;

        ActionVisualization m_ActionViz;

        const float k_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second.

        /// Player characters need to report health changes and chracter info to the PartyHUD
        PartyHUD m_PartyHUD;

        float m_SmoothedSpeed;

        int m_HitStateTriggerID;

        event Action Destroyed;

        public override void NetworkStart()
        {
            if (!IsClient || transform.parent == null)
            {
                enabled = false;
            }

            if (m_BossRoomPlayerCharacter)
            {
                if (m_BossRoomPlayerCharacter.Data)
                {
                    NetworkInitialize();
                }
                else
                {
                    m_BossRoomPlayerCharacter.DataSet += NetworkInitialize;
                    enabled = false;
                }
            }
            else
            {
                NetworkInitialize();
            }
        }

        void NetworkInitialize()
        {
            m_HitStateTriggerID = Animator.StringToHash(ActionFX.k_DefaultHitReact);

            m_ActionViz = new ActionVisualization(this);

            Parent = transform.parent;

            m_NetState = Parent.gameObject.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelAllActionsEventClient += CancelAllActionFXs;
            m_NetState.CancelActionsByTypeEventClient += CancelActionFXByType;
            if (Parent.TryGetComponent(out m_NetworkLifeState))
            {
                m_NetworkLifeState.AddListener(OnLifeStateChanged);
            }
            m_NetState.OnPerformHitReaction += OnPerformHitReaction;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            m_NetState.IsStealthy.OnValueChanged += OnStealthyChanged;

            //we want to follow our parent on a spring, which means it can't be directly in the transform hierarchy.
            Parent.GetComponent<ClientCharacter>().ChildVizObject = this;

            Assert.IsTrue(m_RuntimeObjectsParent && m_RuntimeObjectsParent.Value,
                "RuntimeObjectsParent transform is not set!");
            transform.SetParent(m_RuntimeObjectsParent.Value);

            // sync our visualization position & rotation to the most up to date version received from server
            var parentMovement = Parent.GetComponent<INetMovement>();
            transform.position = parentMovement.NetworkPosition.Value;
            transform.rotation = Quaternion.Euler(0, parentMovement.NetworkRotationY.Value, 0);

            // override our appearance if we are a player
            if (m_BossRoomPlayerCharacter &&
                m_BossRoomPlayerCharacter.Data.TryGetNetworkBehaviour(out m_NetworkAppearanceState))
            {
                // listen for char-select info to change (in practice, this info doesn't
                // change, but we may not have the values set yet) ...
                m_NetworkAppearanceState.AddListener(OnCharacterAppearanceChanged);

                // ...and visualize the current char-select value that we know about
                OnCharacterAppearanceChanged(0, m_NetworkAppearanceState.NetworkCharacterAppearance);
            }

            // ...and visualize the current char-select value that we know about
            SetAppearanceSwap();

            // sync our animator to the most up to date version received from server
            SyncEntryAnimation(m_NetworkLifeState.NetworkLife);

            if (m_BossRoomPlayerCharacter)
            {
                // track health for heroes
                if (m_BossRoomPlayerCharacter.TryGetNetworkBehaviour(out NetworkHealthState networkHealthState))
                {
                    networkHealthState.AddListener(OnHealthChanged);
                }

                string playerName = string.Empty;
                // get player name
                if (m_BossRoomPlayerCharacter.Data.TryGetNetworkBehaviour(out NetworkNameState networkNameState))
                {
                    playerName = networkNameState.NetworkName;
                }

                CharacterTypeEnum characterType = CharacterTypeEnum.Tank;
                // get our character type
                if (m_BossRoomPlayerCharacter.Data.TryGetNetworkBehaviour(out NetworkCharacterTypeState networkCharacterTypeState))
                {
                    // if we are a player, find our character type set from the lobby
                    characterType = networkCharacterTypeState.NetworkCharacterType;
                }

                // find the emote bar to track its buttons
                var partyHudGameObject = GameObject.FindGameObjectWithTag("PartyHUD");
                m_PartyHUD = partyHudGameObject.GetComponent<PartyHUD>();

                if (m_BossRoomPlayerCharacter.Data.IsLocalPlayer)
                {
                    var data = new ActionRequestData { ActionTypeEnum = ActionType.GeneralTarget };
                    m_ActionViz.PlayAction(ref data);
                    gameObject.AddComponent<CameraController>();
                    m_PartyHUD.SetHeroData(m_NetState,
                        m_NetworkAppearanceState.NetworkCharacterAppearance,
                        characterType,
                        networkHealthState.NetworkHealth,
                        playerName);

                    if (Parent.TryGetComponent(out ClientInputSender inputSender))
                    {
                        inputSender.ActionInputEvent += OnActionInput;
                        inputSender.ClientMoveEvent += OnMoveInput;
                    }
                }
                else
                {
                    m_PartyHUD.SetAllyData(m_NetState.NetworkObjectId,
                        characterType,
                        networkHealthState.NetworkHealth,
                        playerName);

                    // getting our parent's NetworkObjectID for PartyHUD removal on Destroy
                    var parentNetworkObjectID = m_NetState.NetworkObjectId;

                    // once this object is destroyed, remove this ally from the PartyHUD UI
                    // NOTE: architecturally this will be refactored
                    Destroyed += () =>
                    {
                        if (m_PartyHUD != null)
                        {
                            m_PartyHUD.RemoveAlly(parentNetworkObjectID);
                        }
                    };
                }
            }

            enabled = true;
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
                    m_ClientVisualsAnimator.SetTrigger(m_VisualizationConfiguration.EntryDeathTriggerID);
                    break;
                case LifeState.Fainted: // ie. PCs already fainted
                    m_ClientVisualsAnimator.SetTrigger(m_VisualizationConfiguration.EntryFaintedTriggerID);
                    break;
            }
        }
        void OnDestroy()
        {
            if (m_NetState)
            {
                m_NetState.DoActionEventClient -= PerformActionFX;
                m_NetState.CancelAllActionsEventClient -= CancelAllActionFXs;
                m_NetState.CancelActionsByTypeEventClient -= CancelActionFXByType;
                if (m_NetworkLifeState)
                {
                    m_NetworkLifeState.RemoveListener(OnLifeStateChanged);
                }
                m_NetState.OnPerformHitReaction -= OnPerformHitReaction;
                m_NetState.OnStopChargingUpClient -= OnStoppedChargingUp;
                m_NetState.IsStealthy.OnValueChanged -= OnStealthyChanged;

                if (Parent != null && Parent.TryGetComponent(out ClientInputSender sender))
                {
                    sender.ActionInputEvent -= OnActionInput;
                    sender.ClientMoveEvent -= OnMoveInput;
                }
            }

            if (m_BossRoomPlayerCharacter)
            {
                m_BossRoomPlayerCharacter.DataSet -= NetworkInitialize;
            }

            Destroyed?.Invoke();
        }

        void OnPerformHitReaction()
        {
            m_ClientVisualsAnimator.SetTrigger(m_HitStateTriggerID);
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

        void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.Alive:
                    m_ClientVisualsAnimator.SetTrigger(m_VisualizationConfiguration.AliveStateTriggerID);
                    break;
                case LifeState.Fainted:
                    m_ClientVisualsAnimator.SetTrigger(m_VisualizationConfiguration.FaintedStateTriggerID);
                    break;
                case LifeState.Dead:
                    m_ClientVisualsAnimator.SetTrigger(m_VisualizationConfiguration.DeadStateTriggerID);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
            }
        }

        void OnHealthChanged(int previousValue, int newValue)
        {
            // don't do anything if party HUD goes away - can happen as Dungeon scene is destroyed
            if (m_PartyHUD == null) { return; }

            if (IsLocalPlayer)
            {
                m_PartyHUD.SetHeroHealth(newValue);
            }
            else
            {
                m_PartyHUD.SetAllyHealth(m_NetState.NetworkObjectId, newValue);
            }
        }

        void OnCharacterAppearanceChanged(int oldValue, int newValue)
        {
            SetAppearanceSwap();
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

                m_CharacterSwapper.SwapToModel(m_NetworkAppearanceState.NetworkCharacterAppearance, specialMaterialMode);
            }
        }

        /// <summary>
        /// Returns the value we should set the Animator's "Speed" variable, given current gameplay conditions.
        /// </summary>
        float GetVisualMovementSpeed()
        {
            Assert.IsNotNull(m_VisualizationConfiguration);
            if (m_NetworkLifeState.NetworkLife != LifeState.Alive)
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

            VisualUtils.SmoothMove(transform, Parent.transform, Time.deltaTime, ref m_SmoothedSpeed, k_MaxRotSpeed);

            if (m_ClientVisualsAnimator)
            {
                // set Animator variables here
                m_ClientVisualsAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, GetVisualMovementSpeed());
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
