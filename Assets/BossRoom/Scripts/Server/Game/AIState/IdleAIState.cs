using UnityEngine;

namespace BossRoom.Server
{
    public class IdleAIState : AIState
    {
        private AIBrain m_brain;

        public IdleAIState(AIBrain brain)
        {
            m_brain = brain;
        }

        public override bool IsEligible()
        {
            return m_brain.GetHatedEnemies().Count == 0;
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
            float detectionRange = m_brain.CharacterData.DetectRange;
            // we are doing this check every Update, so we'll use square-magnitude distance to avoid the expensive sqrt (that's implicit in Vector3.magnitude)
            float detectionRangeSqr = detectionRange * detectionRange;
            Vector3 position = m_brain.GetMyServerCharacter().transform.position;

            foreach (ServerCharacter character in ServerCharacter.GetAllActiveServerCharacters())
            {
                if (m_brain.IsAppropriateFoe(character)
                    && (character.transform.position - position).sqrMagnitude <= detectionRangeSqr)
                {
                    m_brain.Hate(character);
                }
            }
        }
    }
}
