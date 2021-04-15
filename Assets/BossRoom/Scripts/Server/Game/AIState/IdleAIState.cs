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
            float detectionRange = m_Brain.CharacterData.DetectRange;
            // we are doing this check every Update, so we'll use square-magnitude distance to avoid the expensive sqrt (that's implicit in Vector3.magnitude)
            float detectionRangeSqr = detectionRange * detectionRange;
            Vector3 myPosition = m_Brain.GetMyServerCharacter().transform.position;

            // in this game, NPCs only attack players (and never other NPCs), so we can just iterate over the players to see if any are nearby
            foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (m_Brain.IsAppropriateFoe(serverCharacter) && (serverCharacter.transform.position - myPosition).sqrMagnitude <= detectionRangeSqr)
                {
                    m_Brain.Hate(serverCharacter);
                }
            }
        }
    }
}
