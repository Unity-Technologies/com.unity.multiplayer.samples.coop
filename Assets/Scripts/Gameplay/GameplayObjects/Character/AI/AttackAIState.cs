using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.Actions;
using UnityEngine;
using Action = Unity.BossRoom.Gameplay.Actions.Action;
using Random = UnityEngine.Random;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character.AI
{
    public class AttackAIState : AIState
    {
        private AIBrain m_Brain;
        private ServerActionPlayer m_ServerActionPlayer;
        private ServerCharacter m_Foe;
        private Action m_CurAttackAction;

        List<Action> m_AttackActions;

        public AttackAIState(AIBrain brain, ServerActionPlayer serverActionPlayer)
        {
            m_Brain = brain;
            m_ServerActionPlayer = serverActionPlayer;
        }

        public override bool IsEligible()
        {
            return m_Foe != null || ChooseFoe() != null;
        }

        public override void Initialize()
        {
            m_AttackActions = new List<Action>();
            if (m_Brain.CharacterData.Skill1 != null)
            {
                m_AttackActions.Add(m_Brain.CharacterData.Skill1);
            }
            if (m_Brain.CharacterData.Skill2 != null)
            {
                m_AttackActions.Add(m_Brain.CharacterData.Skill2);
            }
            if (m_Brain.CharacterData.Skill3 != null)
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
                m_ServerActionPlayer.ClearActions(true);
            }

            // if we're out of foes, stop! IsEligible() will now return false so we'll soon switch to a new state
            if (!m_Foe)
            {
                return;
            }

            // see if we're already chasing or attacking our active foe!
            if (m_ServerActionPlayer.GetActiveActionInfo(out var info))
            {
                if (GameDataSource.Instance.GetActionPrototypeByID(info.ActionID).IsChaseAction)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == m_Foe.NetworkObjectId)
                    {
                        // yep we're chasing our foe; all set! (The attack is enqueued after it)
                        return;
                    }
                }
                else if (info.ActionID == m_CurAttackAction.ActionID)
                {
                    if (info.TargetIds != null && info.TargetIds[0] == m_Foe.NetworkObjectId)
                    {
                        // yep we're attacking our foe; all set!
                        return;
                    }
                }
                else if (GameDataSource.Instance.GetActionPrototypeByID(info.ActionID).IsStunAction)
                {
                    // we can't do anything right now. We're stunned!
                    return;
                }
            }

            // choose the attack to use
            m_CurAttackAction = ChooseAttack();
            if (m_CurAttackAction == null)
            {
                // no actions are usable right now
                return;
            }

            // attack!
            var attackData = new ActionRequestData
            {
                ActionID = m_CurAttackAction.ActionID,
                TargetIds = new ulong[] { m_Foe.NetworkObjectId },
                ShouldClose = true,
                Direction = m_Brain.GetMyServerCharacter().physicsWrapper.Transform.forward
            };
            m_ServerActionPlayer.PlayAction(ref attackData);
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
        /// Randomly picks a usable attack. If no actions are usable right now, returns null.
        /// </summary>
        /// <returns>Action to attack with, or null</returns>
        private Action ChooseAttack()
        {
            // make a random choice
            int idx = Random.Range(0, m_AttackActions.Count);

            // now iterate through our options to find one that's currently usable
            bool anyUsable;
            do
            {
                anyUsable = false;
                foreach (var attack in m_AttackActions)
                {
                    if (m_ServerActionPlayer.IsReuseTimeElapsed(attack.ActionID))
                    {
                        anyUsable = true;
                        if (idx == 0)
                        {
                            return attack;
                        }
                        --idx;
                    }
                }
            } while (anyUsable);

            // none of our actions are available now
            return null;
        }
    }
}
