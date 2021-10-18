using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public enum LifeState
    {
        Alive,
        Fainted,
        Dead,
    }

    /// <summary>
    /// Describes how the character's movement should be animated: as standing idle, running normally,
    /// magically slowed, sped up, etc. (Not all statuses are currently used by game content,
    /// but they are set up to be displayed correctly for future use.)
    /// </summary>
    [Serializable]
    public enum MovementStatus
    {
        Idle,         // not trying to move
        Normal,       // character is moving (normally)
        Uncontrolled, // character is being moved by e.g. a knockback -- they are not in control!
        Slowed,       // character's movement is magically hindered
        Hasted,       // character's movement is magically enhanced
        Walking,      // character should appear to be "walking" rather than normal running (e.g. for cut-scenes)
    }

    /// <summary>
    /// Contains all NetworkVariables and RPCs of a character. This component is present on both client and server objects.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState))]
    public class NetworkCharacterState : NetworkBehaviour, ITargetable
    {
        /// Indicates how the character's movement should be depicted.
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        /// <summary>
        /// Indicates whether this character is in "stealth mode" (invisible to monsters and other players).
        /// </summary>
        public NetworkVariable<bool> IsStealthy { get; } = new NetworkVariable<bool>();

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        public NetworkHealthState HealthState
        {
            get
            {
                return m_NetworkHealthState;
            }
        }

        /// <summary>
        /// The active target of this character.
        /// </summary>
        public NetworkVariable<ulong> TargetId { get; } = new NetworkVariable<ulong>();

        /// <summary>
        /// Current HP. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public int HitPoints
        {
            get { return m_NetworkHealthState.HitPoints.Value; }
            set { m_NetworkHealthState.HitPoints.Value = value; }
        }

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public NetworkLifeState NetworkLifeState => m_NetworkLifeState;

        /// <summary>
        /// Current LifeState. Only Players should enter the FAINTED state.
        /// </summary>
        public LifeState LifeState
        {
            get => m_NetworkLifeState.LifeState.Value;
            set => m_NetworkLifeState.LifeState.Value = value;
        }

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc { get { return CharacterClass.IsNpc; } }

        public bool IsValidTarget => LifeState != LifeState.Dead;

        /// <summary>
        /// Returns true if the Character is currently in a state where it can play actions, false otherwise.
        /// </summary>
        public bool CanPerformActions => LifeState == LifeState.Alive;

        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        /// <summary>
        /// The CharacterData object associated with this Character. This is the static game data that defines its attack skills, HP, etc.
        /// </summary>
        public CharacterClass CharacterClass => m_CharacterClassContainer.CharacterClass;

        /// <summary>
        /// Character Type. This value is populated during character selection.
        /// </summary>
        public CharacterTypeEnum CharacterType => m_CharacterClassContainer.CharacterClass.CharacterType;

        /// <summary>
        /// Gets invoked when inputs are received from the client which own this networked character.
        /// </summary>
        public event Action<Vector3> ReceivedClientInput;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            HitPoints = CharacterClass.BaseHP.Value;
        }

        /// <summary>
        /// RPC to send inputs for this character from a client to a server.
        /// </summary>
        /// <param name="movementTarget">The position which this character should move towards.</param>
        [ServerRpc]
        public void SendCharacterInputServerRpc(Vector3 movementTarget)
        {
            ReceivedClientInput?.Invoke(movementTarget);
        }

        // ACTION SYSTEM

        /// <summary>
        /// This event is raised on the server when an action request arrives
        /// </summary>
        public event Action<ActionRequestData> DoActionEventServer;

        /// <summary>
        /// This event is raised on the client when an action is being played back.
        /// </summary>
        public event Action<ActionRequestData> DoActionEventClient;

        /// <summary>
        /// This event is raised on the client when the active action FXs need to be cancelled (e.g. when the character has been stunned)
        /// </summary>
        public event Action CancelAllActionsEventClient;

        /// <summary>
        /// This event is raised on the client when active action FXs of a certain type need to be cancelled (e.g. when the Stealth action ends)
        /// </summary>
        public event Action<ActionType> CancelActionsByTypeEventClient;

        /// <summary>
        /// /// Server to Client RPC that broadcasts this action play to all clients.
        /// </summary>
        /// <param name="data"> Data about which action to play and its associated details. </param>
        [ClientRpc]
        public void RecvDoActionClientRPC(ActionRequestData data)
        {
            DoActionEventClient?.Invoke(data);
        }

        [ClientRpc]
        public void RecvCancelAllActionsClientRpc()
        {
            CancelAllActionsEventClient?.Invoke();
        }

        [ClientRpc]
        public void RecvCancelActionsByTypeClientRpc(ActionType action)
        {
            CancelActionsByTypeEventClient?.Invoke(action);
        }

        /// <summary>
        /// Client->Server RPC that sends a request to play an action.
        /// </summary>
        /// <param name="data">Data about which action to play and its associated details. </param>
        [ServerRpc]
        public void RecvDoActionServerRPC(ActionRequestData data)
        {
            DoActionEventServer?.Invoke(data);
        }

        // UTILITY AND SPECIAL-PURPOSE RPCs

        /// <summary>
        /// Called on server when the character's client decides they have stopped "charging up" an attack.
        /// </summary>
        public event Action OnStopChargingUpServer;

        /// <summary>
        /// Called on all clients when this character has stopped "charging up" an attack.
        /// Provides a value between 0 and 1 inclusive which indicates how "charged up" the attack ended up being.
        /// </summary>
        public event Action<float> OnStopChargingUpClient;

        [ServerRpc]
        public void RecvStopChargingUpServerRpc()
        {
            OnStopChargingUpServer?.Invoke();
        }

        [ClientRpc]
        public void RecvStopChargingUpClientRpc(float percentCharged)
        {
            OnStopChargingUpClient?.Invoke(percentCharged);
        }
    }
}
