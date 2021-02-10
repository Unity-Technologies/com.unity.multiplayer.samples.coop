using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    [RequireComponent(typeof(ServerCharacterMovement), typeof(NetworkCharacterState))]
    public class ServerCharacter : MLAPI.NetworkedBehaviour
    {
        public NetworkCharacterState NetState { get; private set; }

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc
        {
            get { return NetState.IsNpc; }
        }

        [SerializeField]
        [Tooltip("If set, the ServerCharacter will automatically play the StartingAction when it is created. ")]
        private ActionType m_StartingAction = ActionType.None;

        [SerializeField]
        [Tooltip("If set to false, an NPC character will be denied its brain (won't attack or chase players)")]
        private bool m_BrainEnabled = true;



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

        void Start()
        {
            NetState = GetComponent<NetworkCharacterState>();
            m_ActionPlayer = new ActionPlayer(this);
            if (IsNpc)
            {
                m_AIBrain = new AIBrain(this, m_ActionPlayer);
            }
        }

        public override void NetworkStart()
        {
            if (!IsServer) { enabled = false; }
            else
            {
                NetState = GetComponent<NetworkCharacterState>();
                NetState.DoActionEventServer += OnActionPlayRequest;
                NetState.ReceivedClientInput += OnClientMoveRequest;
                NetState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;


                NetState.HitPoints.Value = NetState.CharacterData.BaseHP;
                NetState.Mana.Value = NetState.CharacterData.BaseMana;

                if (m_StartingAction != ActionType.None)
                {
                    var startingAction = new ActionRequestData() { ActionTypeEnum = m_StartingAction };
                    PlayAction(ref startingAction);
                }
            }
        }

        /// <summary>
        /// Play an action!
        /// </summary>
        /// <param name="data">Contains all data necessary to create the action</param>
        public void PlayAction(ref ActionRequestData data)
        {
            //the character needs to be alive in order to be able to play actions
            if (NetState.NetworkLifeState.Value == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                if (data.CancelMovement)
                {
                    GetComponent<ServerCharacterMovement>().CancelMove();
                }

                //Can't trust the client! If this was a human request, make sure the Level of the skill being played is correct.
                m_ActionPlayer.PlayAction(ref data);
            }
        }

        private void OnClientMoveRequest(Vector3 targetPosition)
        {
            if (NetState.NetworkLifeState.Value == LifeState.Alive && !m_Movement.IsPerformingForcedMovement())
            {
                ClearActions();
                m_Movement.SetMovementTarget(targetPosition);
            }
        }

        private void OnLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            if (lifeState != LifeState.Alive)
            {
                ClearActions();
                m_Movement.CancelMove();
            }
        }

        /// <summary>
        /// Clear all active Actions. 
        /// </summary>
        public void ClearActions()
        {
            m_ActionPlayer.ClearActions();
        }

        private void OnActionPlayRequest(ActionRequestData data)
        {
            PlayAction(ref data);
        }

        /// <summary>
        /// Receive an HP change from somewhere. Could be healing or damage.
        /// </summary>
        /// <param name="Inflicter">Person dishing out this damage/healing. Can be null. </param>
        /// <param name="HP">The HP to receive. Positive value is healing. Negative is damage.  </param>
        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            //in a more complicated implementation, we might look up all sorts of effects from the inflicter, and compare them
            //to our own effects, and modify the damage or healing as appropriate. But in this game, we just take it straight.

            NetState.HitPoints.Value += HP;

            //we can't currently heal a dead character back to Alive state.
            //that's handled by a separate function.
            if (NetState.HitPoints.Value <= 0)
            {
                ClearActions();

                if (IsNpc)
                {
                    NetState.NetworkLifeState.Value = LifeState.Dead;
                }
                else
                {
                    NetState.NetworkLifeState.Value = LifeState.Fainted;
                }
            }
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
                NetState.HitPoints.Value = HP;
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
            m_ActionPlayer.OnCollisionEnter(collision);
        }
    }
}
