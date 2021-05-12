using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Handles enemy AI. Contains AIStateLogics that handle some of the details,
    /// and has various utility functions that are called by those AIStateLogics
    /// </summary>
    public class AIBrain
    {
        enum AIStateType
        {
            Attack,
            //Wander,
            Idle,
        }

        static readonly AIStateType[] k_AIStates = (AIStateType[])Enum.GetValues(typeof(AIStateType));

        ServerCharacter m_ServerCharacter;
        ActionPlayer m_ActionPlayer;
        AIStateType m_CurrentState;
        Dictionary<AIStateType, AIState> m_Logics;
        List<ServerCharacter> m_HatedEnemies;

        /// <summary>
        /// If we are created by a spawner, the spawner might override our detection radius
        /// -1 is a sentinel value meaning "no override"
        /// </summary>
        float m_DetectRangeOverride = -1;

        /// <summary>
        /// Convenience getter that returns the CharacterData associated with this creature.
        /// </summary>
        public CharacterClass CharacterData { get; }

        /// <summary>
        /// The range at which this character can detect enemies, in meters.
        /// This is usually the same value as is indicated by our game data, but it
        /// can be dynamically overridden.
        /// </summary>
        public float DetectRange
        {
            get => (m_DetectRangeOverride == -1) ? CharacterData.DetectRange : m_DetectRangeOverride;
            set => m_DetectRangeOverride = value;
        }

        /// <summary>
        /// Retrieve info about who we are. Treat as read-only!
        /// </summary>
        /// <returns></returns>
        public ServerCharacter ServerCharacter => m_ServerCharacter;

        public AIBrain(ServerCharacter me, ActionPlayer myActionPlayer)
        {
            m_ServerCharacter = me;
            m_ActionPlayer = myActionPlayer;

            m_Logics = new Dictionary<AIStateType, AIState>
            {
                [AIStateType.Idle] = new IdleAIState(this),
                //[ AIStateType.Wander ] = new WanderAIState(this), // not implemented yet
                [AIStateType.Attack] = new AttackAIState(this, m_ActionPlayer),
            };
            m_HatedEnemies = new List<ServerCharacter>();
            m_CurrentState = AIStateType.Idle;

            CharacterData = me.NetState.CharacterData;
        }

        /// <summary>
        /// Should be called by the AIBrain's owner each Update()
        /// </summary>
        public void Update()
        {
            AIStateType newState = FindBestEligibleAIState();
            if (m_CurrentState != newState)
            {
                m_Logics[newState].Initialize();
            }
            m_CurrentState = newState;
            m_Logics[m_CurrentState].Update();
        }

        /// <summary>
        /// Called when we received some HP. Positive HP is healing, negative is damage.
        /// </summary>
        /// <param name="inflicter">The person who hurt or healed us. May be null. </param>
        /// <param name="amount">The amount of HP received. Negative is damage. </param>
        public void ReceiveHP(ServerCharacter inflicter, int amount)
        {
            if (inflicter != null && amount < 0)
            {
                Hate(inflicter);
            }
        }

        AIStateType FindBestEligibleAIState()
        {
            // for now we assume the AI states are in order of appropriateness,
            // which may be nonsensical when there are more states
            foreach (AIStateType aiStateType in k_AIStates)
            {
                if (m_Logics[aiStateType].IsEligible())
                {
                    return aiStateType;
                }
            }

            Debug.LogError("No AI states are valid!?!");
            return AIStateType.Idle;
        }

        /// <summary>
        /// Returns true if it be appropriate for us to murder this character, starting right now!
        /// </summary>
        public bool IsAppropriateFoe(ServerCharacter potentialFoe)
        {
            if (potentialFoe == null ||
                potentialFoe.IsNpc ||
                potentialFoe.NetworkLifeState.NetworkLife != LifeState.Alive ||
                potentialFoe.NetState.IsStealthy.Value)
            {
                return false;
            }

            // Also, we could use NavMesh.Raycast() to see if we have line of sight to foe?
            return true;
        }

        /// <summary>
        /// Notify the AIBrain that we should consider this character an enemy.
        /// </summary>
        /// <param name="character"></param>
        public void Hate(ServerCharacter character)
        {
            if (!m_HatedEnemies.Contains(character))
            {
                m_HatedEnemies.Add(character);
            }
        }

        /// <summary>
        /// Return the raw list of hated enemies -- treat as read-only!
        /// </summary>
        public List<ServerCharacter> GetHatedEnemies()
        {
            // first we clean the list -- remove any enemies that have disappeared (became null), are dead, etc.
            for (int i = m_HatedEnemies.Count - 1; i >= 0; i--)
            {
                if (!IsAppropriateFoe(m_HatedEnemies[i]))
                {
                    m_HatedEnemies.RemoveAt(i);
                }
            }
            return m_HatedEnemies;
        }
    }
}
