using System.Collections;
using MLAPI;
using UnityEngine;

namespace BossRoom.Server
{
    [RequireComponent(typeof(ServerCharacterMovement), typeof(NetworkCharacterState),
        typeof(NetworkLifeState)), RequireComponent(typeof(NetworkHealthState))]
    public class ServerCharacter : NetworkBehaviour, IDamageable
    {
        [SerializeField]
        BossRoomPlayerCharacter m_BossRoomPlayerCharacter;

        [SerializeField]
        ServerCharacterMovement m_Movement;

        [SerializeField]
        NetworkCharacterState m_NetworkCharacterState;

        public NetworkCharacterState NetState => m_NetworkCharacterState;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public NetworkLifeState NetworkLifeState => m_NetworkLifeState;

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc => m_CharacterData.IsNpc;

        /// <summary>
        /// The Character's ActionPlayer. This is mainly exposed for use by other Actions. In particular, users are discouraged from
        /// calling 'PlayAction' directly on this, as the ServerCharacter has certain game-level checks it performs in its own wrapper.
        /// </summary>
        public ActionPlayer RunningActions => m_ActionPlayer;

        [SerializeField]
        [Tooltip("If set to false, an NPC character will be denied its brain (won't attack or chase players)")]
        bool m_BrainEnabled = true;

        [SerializeField]
        [Tooltip("Setting negative value disables destroying object after it is killed.")]
        float m_KilledDestroyDelaySeconds = 3.0f;

        [SerializeField]
        [Tooltip("If set, the ServerCharacter will automatically play the StartingAction when it is created. ")]
        ActionType m_StartingAction = ActionType.None;

        ActionPlayer m_ActionPlayer;

        /// <summary>
        /// This character's AIBrain. Will be null if this is not an NPC.
        /// </summary>
        public AIBrain AIBrain { get; private set; }

        // This field is exposed in the editor for prefabs which have a non-changing character type (ie. NPCs). For
        // players, this field is set on spawn. This will be refactored further.
        [SerializeField]
        CharacterClass m_CharacterData;

        public override void NetworkStart()
        {
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                if (m_BossRoomPlayerCharacter)
                {
                    if (m_BossRoomPlayerCharacter.BossRoomPlayer)
                    {
                        NetworkInitialize();
                    }
                    else
                    {
                        m_BossRoomPlayerCharacter.BossRoomPlayerNetworkReadied += NetworkInitialize;
                        enabled = false;
                    }
                }
                else
                {
                    NetworkInitialize();
                }
            }
        }

        void NetworkInitialize()
        {
            // create AI classes
            m_ActionPlayer = new ActionPlayer(this);

            if (m_BossRoomPlayerCharacter &&
                m_BossRoomPlayerCharacter.BossRoomPlayer.TryGetNetworkBehaviour(out NetworkCharacterTypeState networkCharacterTypeState))
            {
                m_CharacterData = GameDataSource.Instance.CharacterDataByType[networkCharacterTypeState.NetworkCharacterType];
            }

            if (IsNpc)
            {
                AIBrain = new AIBrain(this, m_ActionPlayer);
            }

            NetState.DoActionEventServer += OnActionPlayRequest;
            NetState.ReceivedClientInput += OnClientMoveRequest;
            NetState.OnStopChargingUpServer += OnStoppedChargingUp;
            m_NetworkLifeState.AddListener(OnLifeStateChanged);

            // assign starting health value
            m_NetworkHealthState.NetworkHealth = m_CharacterData.BaseHP.Value;

            if (m_StartingAction != ActionType.None)
            {
                var startingAction = new ActionRequestData() { ActionTypeEnum = m_StartingAction };
                PlayAction(ref startingAction);
            }

            enabled = true;
        }

        public void OnDestroy()
        {
            if (NetState)
            {
                NetState.DoActionEventServer -= OnActionPlayRequest;
                NetState.ReceivedClientInput -= OnClientMoveRequest;
                NetState.OnStopChargingUpServer -= OnStoppedChargingUp;
                m_NetworkLifeState.AddListener(OnLifeStateChanged);
            }

            if (m_BossRoomPlayerCharacter)
            {
                m_BossRoomPlayerCharacter.BossRoomPlayerNetworkReadied -= NetworkInitialize;
            }
        }

        /// <summary>
        /// Play a sequence of actions!
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            //the character needs to be alive in order to be able to play actions
            if (m_NetworkLifeState.NetworkLife == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                if (action.CancelMovement)
                {
                    m_Movement.CancelMove();
                }

                m_ActionPlayer.PlayAction(ref action);
            }
        }

        void OnClientMoveRequest(Vector3 targetPosition)
        {
            if (m_NetworkLifeState.NetworkLife == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                // if we're currently playing an interruptible action, interrupt it!
                if (m_ActionPlayer.GetActiveActionInfo(out ActionRequestData data))
                {
                    if (GameDataSource.Instance.ActionDataByType.TryGetValue(data.ActionTypeEnum, out ActionDescription description))
                    {
                        if (description.ActionInterruptible)
                        {
                            m_ActionPlayer.ClearActions(false);
                        }
                    }
                }

                m_ActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true); //clear target on move.
                m_Movement.SetMovementTarget(targetPosition);
            }
        }

        void OnLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            if (lifeState != LifeState.Alive)
            {
                m_ActionPlayer.ClearActions(true);
                m_Movement.CancelMove();
            }
        }

        void OnActionPlayRequest(ActionRequestData data)
        {
            if (!GameDataSource.Instance.ActionDataByType[data.ActionTypeEnum].IsFriendly)
            {
                // notify running actions that we're using a new attack. (e.g. so Stealth can cancel itself)
                RunningActions.OnGameplayActivity(Action.GameplayActivity.UsingAttackAction);
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
        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            //to our own effects, and modify the damage or healing as appropriate. But in this game, we just take it straight.
            if (HP > 0)
            {
                m_ActionPlayer.OnGameplayActivity(Action.GameplayActivity.Healed);
                float healingMod = m_ActionPlayer.GetBuffedValue(Action.BuffableValue.PercentHealingReceived);
                HP = (int)(HP * healingMod);
            }
            else
            {
                m_ActionPlayer.OnGameplayActivity(Action.GameplayActivity.AttackedByEnemy);
                float damageMod = m_ActionPlayer.GetBuffedValue(Action.BuffableValue.PercentDamageReceived);
                HP = (int)(HP * damageMod);
            }

            m_NetworkHealthState.NetworkHealth = Mathf.Min(m_CharacterData.BaseHP.Value,
                m_NetworkHealthState.NetworkHealth + HP);

            if( AIBrain != null )
            {
                //let the brain know about the modified amount of damage we received.
                AIBrain.ReceiveHP(inflicter, HP);
            }

            //we can't currently heal a dead character back to Alive state.
            //that's handled by a separate function.
            if (m_NetworkHealthState.NetworkHealth <= 0)
            {
                m_ActionPlayer.ClearActions(false);

                if (IsNpc)
                {
                    if (m_KilledDestroyDelaySeconds >= 0.0f && m_NetworkLifeState.NetworkLife != LifeState.Dead)
                    {
                        StartCoroutine(KilledDestroyProcess());
                    }

                    m_NetworkLifeState.NetworkLife = LifeState.Dead;
                }
                else
                {
                    m_NetworkLifeState.NetworkLife = LifeState.Fainted;
                }
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
            return m_ActionPlayer.GetBuffedValue(buffType);
        }

        /// <summary>
        /// Receive a Life State change that brings Fainted characters back to Alive state.
        /// </summary>
        /// <param name="inflicter">Person reviving the character.</param>
        /// <param name="HP">The HP to set to a newly revived character.</param>
        public void Revive(ServerCharacter inflicter, int HP)
        {
            if (m_NetworkLifeState.NetworkLife == LifeState.Fainted)
            {
                m_NetworkHealthState.NetworkHealth = Mathf.Clamp(HP, 0, m_CharacterData.BaseHP.Value);
                m_NetworkLifeState.NetworkLife = LifeState.Alive;
            }
        }

        void Update()
        {
            if (m_ActionPlayer != null)
            {
                m_ActionPlayer.Update();
            }

            if (AIBrain != null && m_NetworkLifeState.NetworkLife == LifeState.Alive && m_BrainEnabled)
            {
                AIBrain.Update();
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (m_ActionPlayer != null)
            {
                m_ActionPlayer.OnCollisionEnter(collision);
            }
        }

        void OnStoppedChargingUp()
        {
            m_ActionPlayer.OnGameplayActivity(Action.GameplayActivity.StoppedChargingUp);
        }

        public IDamageable.SpecialDamageFlags GetSpecialDamageFlags()
        {
            return IDamageable.SpecialDamageFlags.None;
        }

        public bool IsDamageable()
        {
            return m_NetworkLifeState.NetworkLife == LifeState.Alive;
        }
    }
}
