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
            Idle
        }

        ServerCharacter m_ServerCharacter;
        AIStateType m_CurrentState;
        Dictionary<AIStateType, AIState> m_Logics;
        List<ServerCharacter> m_HatedEnemies;

        public AIBrain(ServerCharacter me, ActionPlayer myActionPlayer)
        {
            m_ServerCharacter = me;

            m_Logics = new Dictionary<AIStateType, AIState>
            {
                [AIStateType.Idle] = new IdleAIState(this),
                //[ AIStateType.Wander ] = new WanderAIState(this), // not written yet
                [AIStateType.Attack] = new AttackAIState(this, myActionPlayer)
            };
            m_HatedEnemies = new List<ServerCharacter>();
            m_CurrentState = AIStateType.Idle;
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
            foreach (AIStateType type in Enum.GetValues(typeof(AIStateType)))
            {
                if (m_Logics[type].IsEligible())
                {
                    return type;
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
                potentialFoe.NetState.NetworkLifeState.Value != LifeState.Alive ||
                potentialFoe.NetState.IsStealthy.Value != 0)
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
            m_HatedEnemies.RemoveAll(enemy => !IsAppropriateFoe(enemy));
            return m_HatedEnemies;
        }

        /// <summary>
        /// Retrieve info about who we are. Treat as read-only!
        /// </summary>
        /// <returns></returns>
        public ServerCharacter GetMyServerCharacter()
        {
            return m_ServerCharacter;
        }

        /// <summary>
        /// Convenience getter that returns the CharacterData associated with this creature.
        /// </summary>
        public CharacterClass CharacterData =>
            GameDataSource.Instance.CharacterDataByType[m_ServerCharacter.NetState.CharacterType];
    }
}
