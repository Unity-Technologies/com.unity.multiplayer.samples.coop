using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Contains all NetworkVariables and RPCs of a character. This component is present on both client and server objects.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState))]
    public class NetworkCharacterState : NetworkBehaviour, ITargetable
    {
        /// Indicates how the character's movement should be depicted.
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        public NetworkVariable<ulong> heldNetworkObject;

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


    }
}
