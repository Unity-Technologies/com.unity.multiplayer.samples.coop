using System;
using System.Collections;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.GameplayObjects.Character.AI;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using Action = Unity.BossRoom.Gameplay.Actions.Action;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Contains all NetworkVariables, RPCs and server-side logic of a character.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState),
        typeof(NetworkLifeState),
        typeof(NetworkAvatarGuidState))]
    public class ServerCharacter : NetworkBehaviour, ITargetable
    {
        [SerializeField]
        ClientCharacterVisualization m_ClientVisualization;

        public ClientCharacterVisualization ClientVisualization => m_ClientVisualization;

        [SerializeField]
        CharacterClass m_CharacterClass;

        public CharacterClass CharacterClass
        {
            get
            {
                if (m_CharacterClass == null)
                {
                    m_CharacterClass = m_State.RegisteredAvatar.CharacterClass;
                }

                return m_CharacterClass;
            }

            set => m_CharacterClass = value;
        }

        NetworkAvatarGuidState m_State;

        /// Indicates how the character's movement should be depicted.
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        public NetworkVariable<ulong> HeldNetworkObject { get; } = new NetworkVariable<ulong>();

        /// <summary>
        /// Indicates whether this character is in "stealth mode" (invisible to monsters and other players).
        /// </summary>
        public NetworkVariable<bool> IsStealthy { get; } = new NetworkVariable<bool>();

        public NetworkHealthState NetHealthState { get; private set; }

        /// <summary>
        /// The active target of this character.
        /// </summary>
        public NetworkVariable<ulong> TargetId { get; } = new NetworkVariable<ulong>();

        /// <summary>
        /// Current HP. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public int HitPoints
        {
            get => NetHealthState.HitPoints.Value;
            private set => NetHealthState.HitPoints.Value = value;
        }

        public NetworkLifeState NetLifeState { get; private set; }

        /// <summary>
        /// Current LifeState. Only Players should enter the FAINTED state.
        /// </summary>
        public LifeState LifeState
        {
            get => NetLifeState.LifeState.Value;
            private set => NetLifeState.LifeState.Value = value;
        }

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc => CharacterClass.IsNpc;

        public bool IsValidTarget => LifeState != LifeState.Dead;

        /// <summary>
        /// Returns true if the Character is currently in a state where it can play actions, false otherwise.
        /// </summary>
        public bool CanPerformActions => LifeState == LifeState.Alive;

        /// <summary>
        /// Character Type. This value is populated during character selection.
        /// </summary>
        public CharacterTypeEnum CharacterType => CharacterClass.CharacterType;

        private ServerActionPlayer m_ServerActionPlayer;

        /// <summary>
        /// The Character's ActionPlayer. This is mainly exposed for use by other Actions. In particular, users are discouraged from
        /// calling 'PlayAction' directly on this, as the ServerCharacter has certain game-level checks it performs in its own wrapper.
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_ServerActionPlayer;

        [SerializeField]
        [Tooltip("If set to false, an NPC character will be denied its brain (won't attack or chase players)")]
        private bool m_BrainEnabled = true;

        [SerializeField]
        [Tooltip("Setting negative value disables destroying object after it is killed.")]
        private float m_KilledDestroyDelaySeconds = 3.0f;

        [SerializeField]
        [Tooltip("If set, the ServerCharacter will automatically play the StartingAction when it is created. ")]
        private Action m_StartingAction;

        private AIBrain m_AIBrain;

        [SerializeField]
        DamageReceiver m_DamageReceiver;

        [SerializeField]
        ServerCharacterMovement m_Movement;

        public ServerCharacterMovement Movement => m_Movement;

        [SerializeField]
        PhysicsWrapper m_PhysicsWrapper;

        public PhysicsWrapper physicsWrapper => m_PhysicsWrapper;

        [SerializeField]
        ServerAnimationHandler m_ServerAnimationHandler;

        public ServerAnimationHandler serverAnimationHandler => m_ServerAnimationHandler;

        private void Awake()
        {
            m_ServerActionPlayer = new ServerActionPlayer(this);
            NetLifeState = GetComponent<NetworkLifeState>();
            NetHealthState = GetComponent<NetworkHealthState>();
            m_State = GetComponent<NetworkAvatarGuidState>();
        }

        public void SetCharacterClass(CharacterClass characterClass)
        {
            m_CharacterClass = characterClass;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) { enabled = false; }
            else
            {
                NetLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
                m_DamageReceiver.DamageReceived += ReceiveHP;
                m_DamageReceiver.CollisionEntered += CollisionEntered;

                if (IsNpc)
                {
                    m_AIBrain = new AIBrain(this, m_ServerActionPlayer);
                }

                if (m_StartingAction != null)
                {
                    var startingAction = new ActionRequestData() { ActionID = m_StartingAction.ActionID };
                    PlayAction(ref startingAction);
                }
                InitializeHitPoints();
            }
        }

        public override void OnNetworkDespawn()
        {
            NetLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;

            if (m_DamageReceiver)
            {
                m_DamageReceiver.DamageReceived -= ReceiveHP;
                m_DamageReceiver.CollisionEntered -= CollisionEntered;
            }
        }


        /// <summary>
        /// RPC to send inputs for this character from a client to a server.
        /// </summary>
        /// <param name="movementTarget">The position which this character should move towards.</param>
        [ServerRpc]
        public void SendCharacterInputServerRpc(Vector3 movementTarget)
        {
            OnClientMoveRequest(movementTarget);
        }

        // ACTION SYSTEM

        /// <summary>
        /// Client->Server RPC that sends a request to play an action.
        /// </summary>
        /// <param name="data">Data about which action to play and its associated details. </param>
        [ServerRpc]
        public void RecvDoActionServerRPC(ActionRequestData data)
        {
            OnActionPlayRequest(data);
        }

        // UTILITY AND SPECIAL-PURPOSE RPCs

        /// <summary>
        /// Called on server when the character's client decides they have stopped "charging up" an attack.
        /// </summary>
        [ServerRpc]
        public void RecvStopChargingUpServerRpc()
        {
            OnStoppedChargingUp();
        }

        void InitializeHitPoints()
        {
            HitPoints = CharacterClass.BaseHP.Value;

            if (!IsNpc)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData is { HasCharacterSpawned: true })
                {
                    HitPoints = sessionPlayerData.Value.CurrentHitPoints;
                    if (HitPoints <= 0)
                    {
                        LifeState = LifeState.Fainted;
                    }
                }
            }
        }

        /// <summary>
        /// Play a sequence of actions!
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            //the character needs to be alive in order to be able to play actions
            if (LifeState == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                if (action.CancelMovement)
                {
                    m_Movement.CancelMove();
                }

                m_ServerActionPlayer.PlayAction(ref action);
            }
        }

        private void OnClientMoveRequest(Vector3 targetPosition)
        {
            if (LifeState == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                // if we're currently playing an interruptible action, interrupt it!
                if (m_ServerActionPlayer.GetActiveActionInfo(out ActionRequestData data))
                {
                    if (GameDataSource.Instance.GetActionPrototypeByID(data.ActionID).Config.ActionInterruptible)
                    {
                        m_ServerActionPlayer.ClearActions(false);
                    }
                }

                m_ServerActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true); //clear target on move.
                m_Movement.SetMovementTarget(targetPosition);
            }
        }

        private void OnLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            if (lifeState != LifeState.Alive)
            {
                m_ServerActionPlayer.ClearActions(true);
                m_Movement.CancelMove();
            }
        }

        private void OnActionPlayRequest(ActionRequestData data)
        {
            if (!GameDataSource.Instance.GetActionPrototypeByID(data.ActionID).Config.IsFriendly)
            {
                // notify running actions that we're using a new attack. (e.g. so Stealth can cancel itself)
                ActionPlayer.OnGameplayActivity(Action.GameplayActivity.UsingAttackAction);
            }

            PlayAction(ref data);
        }

        IEnumerator KilledDestroyProcess()
        {
            yield return new WaitForSeconds(m_KilledDestroyDelaySeconds);

            if (NetworkObject != null)
            {
                NetworkObject.Despawn(true);
            }
        }

        /// <summary>
        /// Receive an HP change from somewhere. Could be healing or damage.
        /// </summary>
        /// <param name="inflicter">Person dishing out this damage/healing. Can be null. </param>
        /// <param name="HP">The HP to receive. Positive value is healing. Negative is damage.  </param>
        void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            //to our own effects, and modify the damage or healing as appropriate. But in this game, we just take it straight.
            if (HP > 0)
            {
                m_ServerActionPlayer.OnGameplayActivity(Action.GameplayActivity.Healed);
                float healingMod = m_ServerActionPlayer.GetBuffedValue(Action.BuffableValue.PercentHealingReceived);
                HP = (int)(HP * healingMod);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Don't apply damage if god mode is on
                if (NetLifeState.IsGodMode.Value)
                {
                    return;
                }
#endif

                m_ServerActionPlayer.OnGameplayActivity(Action.GameplayActivity.AttackedByEnemy);
                float damageMod = m_ServerActionPlayer.GetBuffedValue(Action.BuffableValue.PercentDamageReceived);
                HP = (int)(HP * damageMod);

                serverAnimationHandler.NetworkAnimator.SetTrigger("HitReact1");
            }

            HitPoints = Mathf.Clamp(HitPoints + HP, 0, CharacterClass.BaseHP.Value);

            if (m_AIBrain != null)
            {
                //let the brain know about the modified amount of damage we received.
                m_AIBrain.ReceiveHP(inflicter, HP);
            }

            //we can't currently heal a dead character back to Alive state.
            //that's handled by a separate function.
            if (HitPoints <= 0)
            {
                if (IsNpc)
                {
                    if (m_KilledDestroyDelaySeconds >= 0.0f && LifeState != LifeState.Dead)
                    {
                        StartCoroutine(KilledDestroyProcess());
                    }

                    LifeState = LifeState.Dead;
                }
                else
                {
                    LifeState = LifeState.Fainted;
                }

                m_ServerActionPlayer.ClearActions(false);
            }
        }

        /// <summary>
        /// Determines a gameplay variable for this character. The value is determined
        /// by the character's active Actions.
        /// </summary>
        /// <param name="buffType"></param>
        /// <returns></returns>
        public float GetBuffedValue(Action.BuffableValue buffType)
        {
            return m_ServerActionPlayer.GetBuffedValue(buffType);
        }

        /// <summary>
        /// Receive a Life State change that brings Fainted characters back to Alive state.
        /// </summary>
        /// <param name="inflicter">Person reviving the character.</param>
        /// <param name="HP">The HP to set to a newly revived character.</param>
        public void Revive(ServerCharacter inflicter, int HP)
        {
            if (LifeState == LifeState.Fainted)
            {
                HitPoints = Mathf.Clamp(HP, 0, CharacterClass.BaseHP.Value);
                NetLifeState.LifeState.Value = LifeState.Alive;
            }
        }

        void Update()
        {
            m_ServerActionPlayer.OnUpdate();
            if (m_AIBrain != null && LifeState == LifeState.Alive && m_BrainEnabled)
            {
                m_AIBrain.Update();
            }
        }

        private void CollisionEntered(Collision collision)
        {
            if (m_ServerActionPlayer != null)
            {
                m_ServerActionPlayer.CollisionEntered(collision);
            }
        }

        private void OnStoppedChargingUp()
        {
            m_ServerActionPlayer.OnGameplayActivity(Action.GameplayActivity.StoppedChargingUp);
        }

        /// <summary>
        /// This character's AIBrain. Will be null if this is not an NPC.
        /// </summary>
        public AIBrain AIBrain { get { return m_AIBrain; } }

    }
}
