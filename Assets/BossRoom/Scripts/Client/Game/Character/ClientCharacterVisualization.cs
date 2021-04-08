using System.Collections.Generic;
using BossRoom.Client;
using Cinemachine;
using MLAPI;
using System;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacterVisualization : NetworkBehaviour
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

        public Transform Parent { get; private set; }

        private const float k_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second.

        /// Player characters need to report health changes and chracter info to the PartyHUD
        private Visual.PartyHUD m_PartyHUD;

        private float m_SmoothedSpeed;

        int m_AliveStateTriggerID;
        int m_FaintedStateTriggerID;
        int m_DeadStateTriggerID;
        int m_HitStateTriggerID;
        int m_AnticipateMoveTriggerID;
        int m_SpeedVariableID;

        event Action Destroyed;

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
            m_AnticipateMoveTriggerID = Animator.StringToHash("AnticipateMove");
            m_SpeedVariableID = Animator.StringToHash("Speed");
            m_HitStateTriggerID = Animator.StringToHash(ActionFX.k_DefaultHitReact);

            m_ActionViz = new ActionVisualization(this);

            Parent = transform.parent;

            m_NetState = Parent.gameObject.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionEventClient += PerformActionFX;
            m_NetState.CancelAllActionsEventClient += CancelAllActionFXs;
            m_NetState.CancelActionsByTypeEventClient += CancelActionFXByType;
            m_NetState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;
            m_NetState.OnPerformHitReaction += OnPerformHitReaction;
            m_NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            m_NetState.IsStealthy.OnValueChanged += OnStealthyChanged;

            //we want to follow our parent on a spring, which means it can't be directly in the transform hierarchy.
            Parent.GetComponent<ClientCharacter>().ChildVizObject = this;
            transform.SetParent(null);

            // sync our visualization position & rotation to the most up to date version received from server
            var parentMovement = Parent.GetComponent<INetMovement>();
            transform.position = parentMovement.NetworkPosition.Value;
            transform.rotation = Quaternion.Euler(0, parentMovement.NetworkRotationY.Value, 0);

            // listen for char-select info to change (in practice, this info doesn't
            // change, but we may not have the values set yet) ...
            m_NetState.CharacterAppearance.OnValueChanged += OnCharacterAppearanceChanged;

            // ...and visualize the current char-select value that we know about
            OnCharacterAppearanceChanged(0, m_NetState.CharacterAppearance.Value);

            // ...and visualize the current char-select value that we know about
            SetAppearanceSwap();

            // sync our animator to the most up to date version received from server
            SyncEntryAnimation(m_NetState.NetworkLifeState.Value);

            if (!m_NetState.IsNpc)
            {
                // track health for heroes
                m_NetState.HealthState.HitPoints.OnValueChanged += OnHealthChanged;

                // find the emote bar to track its buttons
                GameObject partyHUDobj = GameObject.FindGameObjectWithTag("PartyHUD");
                m_PartyHUD = partyHUDobj.GetComponent<Visual.PartyHUD>();

                if (IsLocalPlayer)
                {
                    ActionRequestData data = new ActionRequestData { ActionTypeEnum = ActionType.GeneralTarget };
                    m_ActionViz.PlayAction(ref data);
                    gameObject.AddComponent<CameraController>();
                    m_PartyHUD.SetHeroData(m_NetState);

                    if( Parent.TryGetComponent(out ClientInputSender inputSender))
                    {
                        inputSender.ClientMoveEvent += OnMoveInput;
                    }
                }
                else
                {
                    m_PartyHUD.SetAllyData(m_NetState);

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
        }

        private void OnMoveInput(Vector3 position)
        {
            if( !IsAnimating )
            {
                OurAnimator.SetTrigger(m_AnticipateMoveTriggerID);
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
        private void OnDestroy()
        {
            if (m_NetState)
            {
                m_NetState.DoActionEventClient -= PerformActionFX;
                m_NetState.CancelAllActionsEventClient -= CancelAllActionFXs;
                m_NetState.CancelActionsByTypeEventClient -= CancelActionFXByType;
                m_NetState.NetworkLifeState.OnValueChanged -= OnLifeStateChanged;
                m_NetState.OnPerformHitReaction -= OnPerformHitReaction;
                m_NetState.OnStopChargingUpClient -= OnStoppedChargingUp;
                m_NetState.IsStealthy.OnValueChanged -= OnStealthyChanged;

                if (Parent != null && Parent.TryGetComponent(out ClientInputSender sender))
                {
                    sender.ClientMoveEvent -= OnMoveInput;
                }
            }

            Destroyed?.Invoke();
        }

        private void OnPerformHitReaction()
        {
            m_ClientVisualsAnimator.SetTrigger(m_HitStateTriggerID);
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
                this.m_PartyHUD.SetAllyHealth(m_NetState.NetworkObjectId, newValue);
            }
        }

        private void OnCharacterAppearanceChanged(int oldValue, int newValue)
        {
            SetAppearanceSwap();
        }

        private void OnStealthyChanged(bool oldValue, bool newValue)
        {
            SetAppearanceSwap();
        }

        private void SetAppearanceSwap()
        {
            if (m_CharacterSwapper)
            {
                if (m_NetState.IsStealthy.Value && !m_NetState.IsOwner)
                {
                    // this character is in "stealth mode", so other players can't see them!
                    m_CharacterSwapper.SwapAllOff();
                }
                else
                {
                    m_CharacterSwapper.SwapToModel(m_NetState.CharacterAppearance.Value);
                }
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
                float visibleSpeed = 0;
                if (m_NetState.NetworkLifeState.Value == LifeState.Alive)
                {
                    visibleSpeed = m_NetState.VisualMovementSpeed.Value;
                }
                m_ClientVisualsAnimator.SetFloat("Speed", visibleSpeed);
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

        public bool IsAnimating
        {
            get
            {
                if( OurAnimator.GetFloat(m_SpeedVariableID) > 0.0 ) { return true; }

                for( int i = 0; i < OurAnimator.layerCount; i++ )
                {
                    if (!OurAnimator.GetCurrentAnimatorStateInfo(i).IsTag("BaseNode"))
                    {
                        //we are in an active node, not the default "nothing" node.
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
