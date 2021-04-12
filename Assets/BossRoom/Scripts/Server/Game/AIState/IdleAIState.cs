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
            Vector3 position = m_Brain.GetMyServerCharacter().transform.position;

            foreach (var spawnedObject in MLAPI.Spawning.NetworkSpawnManager.SpawnedObjectsList)
            {
                if (!spawnedObject) { continue; } // must have been Destroy()ed very recently
                if ((spawnedObject.transform.position - position).sqrMagnitude <= detectionRangeSqr)
                {
                    // they're within range... are they an appropriate foe?
                    ServerCharacter serverChar = spawnedObject.GetComponent<ServerCharacter>();
                    if (!serverChar) { continue; } // not even a character at all...
                    if (m_Brain.IsAppropriateFoe(serverChar))
                    {
                        m_Brain.Hate(serverChar);
                    }
                }
            }
        }
    }
}
