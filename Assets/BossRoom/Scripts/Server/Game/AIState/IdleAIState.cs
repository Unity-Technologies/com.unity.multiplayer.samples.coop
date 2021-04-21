using MLAPI;
using UnityEngine;

namespace BossRoom.Server
{
    public class IdleAIState : AIState
    {
        private AIBrain m_Brain;

        public IdleAIState(AIBrain brain)
        {
            m_Brain = brain;
        }

        public override bool IsEligible()
        {
            return m_Brain.GetHatedEnemies().Count == 0;
        }

        public override void Initialize()
        {
        }

        public override void Update()
        {
            // while idle, we are scanning for jerks to hate
            DetectFoes();
        }

        protected void DetectFoes()
        {
            // we are doing this check every Update, so we'll use square-magnitude distance to avoid the expensive sqrt (that's implicit in Vector3.magnitude)
            float detectionRangeSqr = m_Brain.DetectRange * m_Brain.DetectRange;
            Vector3 position = m_Brain.GetMyServerCharacter().transform.position;

            // in this game, NPCs only attack players (and never other NPCs), so we can just iterate over the players to see if any are nearby
            foreach (var character in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (m_Brain.IsAppropriateFoe(character) && (character.transform.position - position).sqrMagnitude <= detectionRangeSqr)
                {
                    m_Brain.Hate(character);
                }
            }
        }
    }
}
