using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class AttackAIState : AIState
    {
        private AIBrain m_Brain;
        private ActionPlayer m_ActionPlayer;
        private ServerCharacter m_Foe;
        private ActionType m_CurAttackAction;

        List<ActionType> m_AttackActions;

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
            m_AttackActions = new List<ActionType>();
            if (m_Brain.CharacterData.Skill1 != ActionType.None)
            {
                m_AttackActions.Add(m_Brain.CharacterData.Skill1);
            }
            if (m_Brain.CharacterData.Skill2 != ActionType.None)
            {
                m_AttackActions.Add(m_Brain.CharacterData.Skill2);
            }
            if (m_Brain.CharacterData.Skill3 != ActionType.None)
            {
                m_AttackActions.Add(m_Brain.CharacterData.Skill3);
            }

            // pick a starting attack action from the possible
            m_CurAttackAction = m_AttackActions[Random.Range(0, m_AttackActions.Count)];

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
                m_ActionPlayer.ClearActions(true);
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
                    if (info.TargetIds != null && info.TargetIds[0] == m_Foe.NetworkObjectId)
                    {
                        // yep we're chasing our foe; all set! (The attack is enqueued after it)
                        return;
                    }
                }
                else if (info.ActionTypeEnum == m_CurAttackAction)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == m_Foe.NetworkObjectId)
                    {
                        // yep we're attacking our foe; all set!
                        return;
                    }
                }
                else if (info.ActionTypeEnum == ActionType.Stun)
                {
                    // we can't do anything right now. We're stunned!
                    return;
                }
            }

            // choose the attack to use
            m_CurAttackAction = ChooseAttack();
            if (m_CurAttackAction == ActionType.None)
            {
                // no actions are usable right now
                return;
            }

            // attack!
            var attackData = new ActionRequestData
            {
                ActionTypeEnum = m_CurAttackAction,
                TargetIds = new ulong[] { m_Foe.NetworkObjectId },
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
            Vector3 myPosition = m_Brain.GetMyServerCharacter().physicsWrapper.Transform.position;

            float closestDistanceSqr = int.MaxValue;
            ServerCharacter closestFoe = null;
            foreach (var foe in m_Brain.GetHatedEnemies())
            {
                float distanceSqr = (myPosition - foe.physicsWrapper.Transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestFoe = foe;
                }
            }
            return closestFoe;
        }

        /// <summary>
        /// Randomly picks a usable attack. If no actions are usable right now, returns ActionType.None.
        /// </summary>
        /// <returns>Action to attack with, or ActionType.None</returns>
        private ActionType ChooseAttack()
        {
            // make a random choice
            int idx = Random.Range(0, m_AttackActions.Count);

            // now iterate through our options to find one that's currently usable
            bool anyUsable;
            do
            {
                anyUsable = false;
                foreach (var actionType in m_AttackActions)
                {
                    if (m_ActionPlayer.IsReuseTimeElapsed(actionType))
                    {
                        anyUsable = true;
                        if (idx == 0)
                        {
                            return actionType;
                        }
                        --idx;
                    }
                }
            } while (anyUsable);

            // none of our actions are available now
            return ActionType.None;
        }
    }
}
