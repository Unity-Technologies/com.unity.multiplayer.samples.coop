using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using System;
using UnityEngine;

namespace BossRoom
{
    public enum LifeState
    {
        Alive,
        Fainted,
        Dead,
    }

    /// <summary>
    /// Contains all NetworkVariables and RPCs of a character. This component is present on both client and server objects.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState), typeof(NetworkCharacterTypeState))]
    public class NetworkCharacterState : NetworkBehaviour, INetMovement, ITargetable
    {
        public void InitNetworkPositionAndRotationY(Vector3 initPosition, float initRotationY)
        {
            NetworkPosition.Value = initPosition;
            NetworkRotationY.Value = initRotationY;
        }

        /// <summary>
        /// The networked position of this Character. This reflects the authoritative position on the server.
        /// </summary>
        public NetworkVariableVector3 NetworkPosition { get; } = new NetworkVariableVector3();

        /// <summary>
        /// The networked rotation of this Character. This reflects the authoritative rotation on the server.
        /// </summary>
        public NetworkVariableFloat NetworkRotationY { get; } = new NetworkVariableFloat();

        /// <summary>
        /// The speed that the character is currently allowed to move, according to the server.
        /// </summary>
        public NetworkVariableFloat NetworkMovementSpeed { get; } = new NetworkVariableFloat();

        /// <summary>
        /// Used by animations. Indicates how fast the character should "look like" they're moving,
        /// according to the server. This is a value from 0 (not moving) to 1 (moving as fast as possible).
        /// This does not always correspond to the NetworkMovementSpeed; for instance when a player is being
        /// "knocked back", they are moving, but they visually look like they're standing still.
        /// </summary>
        public NetworkVariableFloat VisualMovementSpeed { get; } = new NetworkVariableFloat();

        /// <summary>
        /// Indicates whether this character is in "stealth mode" (invisible to monsters and other players).
        /// </summary>
        public NetworkVariableBool IsStealthy { get; } = new NetworkVariableBool();

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
        public NetworkVariableULong TargetId { get; } = new NetworkVariableULong();

        /// <summary>
        /// Current HP. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public int HitPoints
        {
            get { return m_NetworkHealthState.HitPoints.Value; }
            set { m_NetworkHealthState.HitPoints.Value = value; }
        }

        /// Current Mana. This value is populated at startup time from CharacterClass data.
        /// </summary>
        [HideInInspector]
        public NetworkVariableInt Mana;

        /// <summary>
        /// Current LifeState. Only Players should enter the FAINTED state.
        /// </summary>
        public NetworkVariable<LifeState> NetworkLifeState { get; } = new NetworkVariable<LifeState>(LifeState.Alive);

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        public bool IsNpc { get { return CharacterData.IsNpc; } }

        public bool IsValidTarget { get { return NetworkLifeState.Value != LifeState.Dead; } }

        /// <summary>
        /// The CharacterData object associated with this Character. This is the static game data that defines its attack skills, HP, etc.
        /// </summary>
        public CharacterClass CharacterData
        {
            get
            {
                return GameDataSource.Instance.CharacterDataByType[CharacterType];
            }
        }

        [SerializeField]
        NetworkCharacterTypeState m_NetworkCharacterTypeState;

        /// <summary>
        /// Character Type. This value is populated during character selection.
        /// </summary>
        public CharacterTypeEnum CharacterType
        {
            get { return m_NetworkCharacterTypeState.CharacterType.Value; }
            set { m_NetworkCharacterTypeState.CharacterType.Value = value; }
        }

        [SerializeField]
        NetworkNameState m_NetworkNameState;

        /// <summary>
        /// Current nametag. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public string Name
        {
            get { return m_NetworkNameState.Name.Value; }
            set { m_NetworkNameState.Name.Value = value; }
        }

        /// <summary>
        /// This is an int rather than an enum because it is a "place-marker" for a more complicated system. Ultimately we would like
        /// PCs to represent their appearance via a struct of appearance options (so they can mix-and-match different ears, head, face, etc).
        /// </summary>
        [Tooltip("Value between 0-7. ClientCharacterVisualization will use this to set up the model (for PCs).")]
        public NetworkVariableInt CharacterAppearance;

        /// <summary>
        /// Gets invoked when inputs are received from the client which own this networked character.
        /// </summary>
        public event Action<Vector3> ReceivedClientInput;

        /// <summary>
        /// RPC to send inputs for this character from a client to a server.
        /// </summary>
        /// <param name="movementTarget">The position which this character should move towards.</param>
        [ServerRpc]
        public void SendCharacterInputServerRpc(Vector3 movementTarget)
        {
            ReceivedClientInput?.Invoke(movementTarget);
        }

        public void SetCharacterType(CharacterTypeEnum playerType, int playerAppearance)
        {
            CharacterType = playerType;
            CharacterAppearance.Value = playerAppearance;
        }

        public void ApplyCharacterData()
        {
            HitPoints = CharacterData.BaseHP.Value;
            Mana.Value = CharacterData.BaseMana;
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

        [ClientRpc]
        /// Server->Client RPC that broadcasts this action play to all clients.
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
        /// Called when the character needs to perform a one-off "I've been hit" animation.
        /// </summary>
        public event Action OnPerformHitReaction;

        /// <summary>
        /// Called by Actions when this character needs to perform a one-off "ouch" reaction-animation.
        /// Note: this is not the normal way to trigger hit-react animations! Normally the client-side
        /// ActionFX directly controls animation. But some Actions can have unpredictable targets. In cases
        /// where the ActionFX can't predict who gets hit, the Action calls this to manually trigger animation.
        /// </summary>
        [ClientRpc]
        public void RecvPerformHitReactionClientRPC()
        {
            OnPerformHitReaction?.Invoke();
        }

        /// <summary>
        /// Called on server when the character's client decides they have stopped "charging up" an attack.
        /// </summary>
        public event Action OnStopChargingUpServer;

        /// <summary>
        /// Called on all clients when this character has stopped "charging up" an attack.
        /// </summary>
        public event Action OnStopChargingUpClient;

        [ServerRpc]
        public void RecvStopChargingUpServerRpc()
        {
            OnStopChargingUpServer?.Invoke();
        }

        [ClientRpc]
        public void RecvStopChargingUpClientRpc()
        {
            OnStopChargingUpClient?.Invoke();
        }
    }
}
