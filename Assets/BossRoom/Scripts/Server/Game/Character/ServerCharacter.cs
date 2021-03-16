using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;

namespace BossRoom.Server
{
    [RequireComponent(typeof(ServerCharacterMovement), typeof(NetworkCharacterState))]
    public class ServerCharacter : NetworkBehaviour, IDamageable
    {
        public NetworkCharacterState NetState { get; private set; }

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc
        {
            get { return NetState.IsNpc; }
        }

        /// <summary>
        /// The Character's ActionPlayer. This is mainly exposed for use by other Actions. In particular, users are discouraged from
        /// calling 'PlayAction' directly on this, as the ServerCharacter has certain game-level checks it performs in its own wrapper.
        /// </summary>
        public ActionPlayer RunningActions {  get { return m_ActionPlayer;  } }

        [SerializeField]
        [Tooltip("If set to false, an NPC character will be denied its brain (won't attack or chase players)")]
        private bool m_BrainEnabled = true;

        [SerializeField]
        [Tooltip("Setting negative value disables destroying object after it is killed.")]
        private float m_KilledDestroyDelaySeconds = 3.0f;

        [SerializeField]
        [Tooltip("If set, the ServerCharacter will automatically play the StartingAction when it is created. ")]
        private ActionType m_StartingAction = ActionType.None;

        private ActionPlayer m_ActionPlayer;
        private AIBrain m_AIBrain;

        // Cached component reference
        private ServerCharacterMovement m_Movement;

        /// <summary>
        /// Temp place to store all the active characters (to avoid having to
        /// perform insanely-expensive GameObject.Find operations during Update)
        /// </summary>
        private static List<ServerCharacter> s_ActiveServerCharacters = new List<ServerCharacter>();

        private void Awake()
        {
            m_Movement = GetComponent<ServerCharacterMovement>();

            NetState = GetComponent<NetworkCharacterState>();
            m_ActionPlayer = new ActionPlayer(this);
            if (IsNpc)
            {
                m_AIBrain = new AIBrain(this, m_ActionPlayer);
            }
        }

        private void OnEnable()
        {
            s_ActiveServerCharacters.Add(this);
        }

        private void OnDisable()
        {
            s_ActiveServerCharacters.Remove(this);
        }

        public static List<ServerCharacter> GetAllActiveServerCharacters()
        {
            return s_ActiveServerCharacters;
        }

        public override void NetworkStart()
        {
            if (!IsServer) { enabled = false; }
            else
            {
                NetState = GetComponent<NetworkCharacterState>();
                NetState.DoActionEventServer += OnActionPlayRequest;
                NetState.ReceivedClientInput += OnClientMoveRequest;
                NetState.OnStopChargingUpServer += OnStoppedChargingUp;
                NetState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;


                NetState.ApplyCharacterData();

                if (m_StartingAction != ActionType.None)
                {
                    var startingAction = new ActionRequestData() { ActionTypeEnum = m_StartingAction };
                    PlayAction(ref startingAction);
                }
            }
        }

        public void OnDestroy()
        {
            if (NetState)
            {
                NetState.DoActionEventServer -= OnActionPlayRequest;
                NetState.ReceivedClientInput -= OnClientMoveRequest;
                NetState.OnStopChargingUpServer -= OnStoppedChargingUp;
                NetState.NetworkLifeState.OnValueChanged -= OnLifeStateChanged;
            }
        }

        /// <summary>
        /// Play a sequence of actions!
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            //the character needs to be alive in order to be able to play actions
            if (NetState.NetworkLifeState.Value == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                if (action.CancelMovement)
                {
                    m_Movement.CancelMove();
                }

                m_ActionPlayer.PlayAction(ref action);
            }
        }

        private void OnClientMoveRequest(Vector3 targetPosition)
        {
            if (NetState.NetworkLifeState.Value == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                ClearActions(false);
                m_ActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true); //clear target on move.
                m_Movement.SetMovementTarget(targetPosition);
            }
        }

        private void OnLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            if (lifeState != LifeState.Alive)
            {
                ClearActions(true);
                m_Movement.CancelMove();
            }
        }

        /// <summary>
        /// Clear all active Actions.
        /// </summary>
        public void ClearActions(bool alsoClearNonBlockingActions)
        {
            m_ActionPlayer.ClearActions(alsoClearNonBlockingActions);
        }

        private void OnActionPlayRequest(ActionRequestData data)
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

            NetState.HitPoints = Mathf.Min(NetState.CharacterData.BaseHP.Value, NetState.HitPoints+HP);

            if( m_AIBrain != null )
            {
                //let the brain know about the modified amount of damage we received. 
                m_AIBrain.ReceiveHP(inflicter, HP);
            }

            //we can't currently heal a dead character back to Alive state.
            //that's handled by a separate function.
            if (NetState.HitPoints <= 0)
            {
                ClearActions(false);

                if (IsNpc)
                {
                    if (m_KilledDestroyDelaySeconds >= 0.0f && NetState.NetworkLifeState.Value != LifeState.Dead)
                    {
                        StartCoroutine(KilledDestroyProcess());
                    }

                    NetState.NetworkLifeState.Value = LifeState.Dead;
                }
                else
                {
                    NetState.NetworkLifeState.Value = LifeState.Fainted;
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
            if (NetState.NetworkLifeState.Value == LifeState.Fainted)
            {
                NetState.HitPoints = NetState.CharacterData.BaseHP.Value;
                NetState.NetworkLifeState.Value = LifeState.Alive;
            }
        }

        void Update()
        {
            m_ActionPlayer.Update();
            if (m_AIBrain != null && NetState.NetworkLifeState.Value == LifeState.Alive && m_BrainEnabled)
            {
                m_AIBrain.Update();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_ActionPlayer != null)
            {
                m_ActionPlayer.OnCollisionEnter(collision);
            }
        }

        private void OnStoppedChargingUp()
        {
            m_ActionPlayer.OnGameplayActivity(Action.GameplayActivity.StoppedChargingUp);
        }
    }
}
