using UnityEngine;

namespace BossRoom.Server
{
    public class AttackAIState : AIState
    {
        private AIBrain m_Brain;
        private ActionPlayer m_ActionPlayer;
        private ServerCharacter m_Foe;
        private ActionType m_CurAttackAction;

        public AttackAIState(AIBrain brain, ActionPlayer actionPlayer)
        {
            m_Brain = brain;
            m_ActionPlayer = actionPlayer;
        }

        public override bool IsEligible()
        {
            return m_Foe != null || ChooseFoe() != null;
        }

        public override void Initialize()
        {
            m_CurAttackAction = m_Brain.CharacterData.Skill1;

            // clear any old foe info; we'll choose a new one in Update()
            m_Foe = null;
        }

        public override void Update()
        {
            if (!m_Brain.IsAppropriateFoe(m_Foe))
            {
                // time for a new foe!
                m_Foe = ChooseFoe();
                // whatever we used to be doing, stop that. New plan is coming!
                m_ActionPlayer.ClearActions();
            }

            // if we're out of foes, stop! IsEligible() will now return false so we'll soon switch to a new state
            if (!m_Foe)
            {
                return;
            }

            // see if we're already chasing or attacking our active foe!
            if (m_ActionPlayer.GetActiveActionInfo(out var info))
            {
                if (info.ActionTypeEnum == ActionType.GeneralChase)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == m_Foe.NetworkId)
                    {
                        // yep we're chasing our foe; all set! (The attack is enqueued after it)
                        return;
                    }
                }
                else if (info.ActionTypeEnum == m_CurAttackAction)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == m_Foe.NetworkId)
                    {
                        // yep we're attacking our foe; all set!
                        return;
                    }
                }
            }

            // Choose whether we can attack our foe directly, or if we need to get closer first
            var attackInfo = GetCurrentAttackInfo();
            var attackData = new ActionRequestData
            {
                ActionTypeEnum = attackInfo.ActionTypeEnum,
                TargetIds = new ulong[] { m_Foe.NetworkId },
                ShouldClose = true
            };
            m_ActionPlayer.PlayAction(ref attackData);
        }

        /// <summary>
        /// Picks the most appropriate foe for us to attack right now, or null if none are appropriate
        /// (Currently just chooses the foe closest to us in distance)
        /// </summary>
        /// <returns></returns>
        private ServerCharacter ChooseFoe()
        {
            Vector3 myPosition = m_Brain.GetMyServerCharacter().transform.position;

            float closestDistanceSqr = int.MaxValue;
            ServerCharacter closestFoe = null;
            foreach (var foe in m_Brain.GetHatedEnemies())
            {
                float distanceSqr = (myPosition - foe.transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestFoe = foe;
                }
            }
            return closestFoe;
        }


        private ActionDescription GetCurrentAttackInfo()
        {
            ActionDescription result;
            bool found = GameDataSource.Instance.ActionDataByType.TryGetValue(m_CurAttackAction, out result);
            if (!found)
            {
                throw new System.Exception($"GameObject {m_Brain.GetMyServerCharacter().gameObject.name} tried to play Action {m_CurAttackAction} but this action does not exist");
            }

            return result;
        }
    }
}
