using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    public class AttackAIState : AIState
    {
        private AIBrain m_brain;
        private ActionPlayer m_actionPlayer;
        private ServerCharacter m_foe;
        private ActionType m_curAttackAction;

        public AttackAIState(AIBrain brain, ActionPlayer actionPlayer)
        {
            m_brain = brain;
            m_actionPlayer = actionPlayer;
        }

        public override bool IsEligible()
        {
            return m_foe != null || ChooseFoe() != null;
        }

        public override void Initialize()
        {
            m_curAttackAction = ActionType.TANK_BASEATTACK;

            // clear any old foe info; we'll choose a new one in Update()
            m_foe = null;
        }

        public override void Update()
        {
            if (!m_brain.IsAppropriateFoe(m_foe))
            {
                // time for a new foe!
                m_foe = ChooseFoe();
                // whatever we used to be doing, stop that. New plan is coming!
                m_actionPlayer.ClearActions();
            }

            // if we're out of foes, stop! IsEligible() will now return false so we'll soon switch to a new state
            if (!m_foe)
            {
                return;
            }

            // see if we're already chasing or attacking our active foe!
            if (m_actionPlayer.GetActiveActionInfo(out var info))
            {
                if (info.ActionTypeEnum == ActionType.GENERAL_CHASE)
                {
                    foreach (var id in info.TargetIds)
                    {
                        if (id == m_foe.NetworkId)
                        {
                            // yep we're chasing our foe; all set! (The attack is enqueued after it)
                            return;
                        }
                    }
                }
                else if (info.ActionTypeEnum == m_curAttackAction)
                {
                    foreach (var id in info.TargetIds)
                    {
                        if (id == m_foe.NetworkId)
                        {
                            // yep we're attacking our foe; all set!
                            return;
                        }
                    }
                }
            }

            // Choose whether we can attack our foe directly, or if we need to get closer first
            var attackInfo = GetCurrentAttackInfo();
            Vector3 diff = m_brain.GetMyServerCharacter().transform.position - m_foe.transform.position;
            if (diff.sqrMagnitude < attackInfo.Range * attackInfo.Range)
            {
                // yes! We are in range
                var attack_data = new ActionRequestData
                {
                    ActionTypeEnum = m_curAttackAction,
                    Amount = attackInfo.Amount,
                    ShouldQueue = false,
                    TargetIds = new ulong[] { m_foe.NetworkId }
                };
                m_actionPlayer.PlayAction(ref attack_data);
            }
            else
            {
                // we are not in range so we will need to chase them
                var chase_data = new ActionRequestData
                {
                    ActionTypeEnum = ActionType.GENERAL_CHASE,
                    Amount = attackInfo.Range,
                    ShouldQueue = false,
                    TargetIds = new ulong[] { m_foe.NetworkId }
                };
                m_actionPlayer.PlayAction(ref chase_data);

                // queue up the actual attack for when we're in range
                var attack_data = new ActionRequestData
                {
                    ActionTypeEnum = m_curAttackAction,
                    Amount = attackInfo.Amount,
                    ShouldQueue = true,
                    TargetIds = new ulong[] { m_foe.NetworkId }
                };
                m_actionPlayer.PlayAction(ref attack_data);
            }
        }

        /// <summary>
        /// Picks the most appropriate foe for us to attack right now, or null if none are appropriate
        /// (Currently just chooses the foe closest to us in distance)
        /// </summary>
        /// <returns></returns>
        private ServerCharacter ChooseFoe()
        {
            Vector3 myPosition = m_brain.GetMyServerCharacter().transform.position;

            float closestDistanceSqr = int.MaxValue;
            ServerCharacter closestFoe = null;
            foreach (var foe in m_brain.GetHatedEnemies())
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
            List<ActionDescription> actionLevels = ActionData.ActionDescriptions[ m_curAttackAction ];
            int level = 0; // FIXME: pull this level from some character state var?
            return actionLevels[ level ];
        }
    }
}