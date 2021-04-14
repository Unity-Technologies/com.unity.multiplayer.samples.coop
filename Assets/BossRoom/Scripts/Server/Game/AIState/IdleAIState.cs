using MLAPI;
using MLAPI.Connection;
using System.Collections.Generic;
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

            // in this game, NPCs only attack players (and never other NPCs), so we can just iterate over the connected players to see if any are nearby
            foreach (var networkClient in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (networkClient.PlayerObject == null)
                {
                    // skip over any connection that doesn't have a PlayerObject yet
                    continue;
                }

                if ((networkClient.PlayerObject.transform.position - myPosition).sqrMagnitude <= detectionRangeSqr)
                {
                    // they're in range! Make sure that they're actually an enemy
                    var serverCharacter = networkClient.PlayerObject.GetComponent<ServerCharacter>();
                    if (serverCharacter && m_Brain.IsAppropriateFoe(serverCharacter))
                    {
                        m_Brain.Hate(serverCharacter);
                    }
                }
            }
        }
    }
}
