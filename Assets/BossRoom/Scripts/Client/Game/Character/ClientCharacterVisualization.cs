using System;
using BossRoom.Client;
using MLAPI;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacterVisualization : NetworkBehaviour
    {
        NetworkCharacterState m_NetState;

        [SerializeField]
        Animator m_ClientVisualsAnimator;

        [SerializeField]
        CharacterSwap m_CharacterSwapper;

        [Tooltip("Prefab for the Target Reticule used by this Character")]
        public GameObject TargetReticule;

        [Tooltip("Material to use when displaying a friendly target reticule (e.g. green color)")]
        public Material ReticuleFriendlyMat;

        [Tooltip("Material to use when displaying a hostile target reticule (e.g. red color)")]
        public Material ReticuleHostileMat;

        public Animator OurAnimator => m_ClientVisualsAnimator;

        ActionVisualization m_ActionVisualization;

        public Transform Parent { get; private set; }

        const float k_MaxRotSpeed = 280;  //max angular speed at which we will rotate, in degrees/second.

        /// Player characters need to report health changes and character info to the PartyHUD
        PartyHUD m_PartyHud;

        float m_SmoothedSpeed;

        int m_AliveStateTriggerID;
        int m_FaintedStateTriggerID;
        int m_DeadStateTriggerID;
        int m_HitStateTriggerID;
        int m_AnticipateMoveTriggerID;
        int m_SpeedVariableID;

        const string k_BaseNodeTag = "BaseNode";

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

            m_ActionVisualization = new ActionVisualization(this);

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
                var partyHudGameObject = GameObject.FindGameObjectWithTag("PartyHUD");
                m_PartyHud = partyHudGameObject.GetComponent<PartyHUD>();

                if (IsLocalPlayer)
                {
                    var data = new ActionRequestData { ActionTypeEnum = ActionType.GeneralTarget };
                    m_ActionVisualization.PlayAction(ref data);
                    gameObject.AddComponent<CameraController>();
                    m_PartyHud.SetHeroData(m_NetState);

                    if( Parent.TryGetComponent(out ClientInputSender inputSender))
                    {
                        inputSender.ClientMoveRequested += OnMoveInput;
                    }
                }
                else
                {
                    m_PartyHud.SetAllyData(m_NetState);
                }
            }
        }

        void OnMoveInput(Vector3 position)
        {
            if (!IsAnimating)
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

        void OnDestroy()
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
                    sender.ClientMoveRequested -= OnMoveInput;
                }
            }
        }

        void OnPerformHitReaction()
        {
            m_ClientVisualsAnimator.SetTrigger(m_HitStateTriggerID);
        }

        void PerformActionFX(ActionRequestData data)
        {
            m_ActionVisualization.PlayAction(ref data);
        }

        void CancelAllActionFXs()
        {
            m_ActionVisualization.CancelAllActions();
        }

        void CancelActionFXByType(ActionType actionType)
        {
            m_ActionVisualization.CancelAllActionsOfType(actionType);
        }

        void OnStoppedChargingUp()
        {
            m_ActionVisualization.OnStoppedChargingUp();
        }

        void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
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

        void OnHealthChanged(int previousValue, int newValue)
        {
            // don't do anything if party HUD goes away - can happen as Dungeon scene is destroyed
            if (m_PartyHud == null) { return; }

            if (IsLocalPlayer)
            {
                m_PartyHud.SetHeroHealth(newValue);
            }
            else
            {
                m_PartyHud.SetAllyHealth(m_NetState.NetworkObjectId, newValue);
            }
        }

        void OnCharacterAppearanceChanged(int oldValue, int newValue)
        {
            SetAppearanceSwap();
        }

        void OnStealthyChanged(byte oldValue, byte newValue)
        {
            SetAppearanceSwap();
        }

        void SetAppearanceSwap()
        {
            if (m_CharacterSwapper)
            {
                if (m_NetState.IsStealthy.Value != 0 && !m_NetState.IsOwner)
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
                m_ClientVisualsAnimator.SetFloat(m_SpeedVariableID, visibleSpeed);
            }

            m_ActionVisualization.Update();
        }

        public void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured.

            m_ActionVisualization.OnAnimEvent(id);
        }

        bool IsAnimating
        {
            get
            {
                if( OurAnimator.GetFloat(m_SpeedVariableID) > 0.0 ) { return true; }

                for( int i = 0; i < OurAnimator.layerCount; i++ )
                {
                    if (!OurAnimator.GetCurrentAnimatorStateInfo(i).IsTag(k_BaseNodeTag))
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
