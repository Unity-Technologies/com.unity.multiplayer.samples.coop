using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Class responsible for playing back action inputs from user. 
    /// </summary>
    public class ActionPlayer
    {
        private ServerCharacter m_Parent;

        private List<Action> m_Queue;

        private List<Action> m_NonBlockingActions;

        /// <summary>
        /// To prevent identical actions from piling up, we start discarding actions that are identical to the last played one
        /// if the queue is deeper than this number. It's a soft cap in that longer queues are possible if they are made of different
        /// actions--this is mainly targeted at situations like melee attacks, where many may get spammed out quickly. 
        /// </summary>
        private const int k_QueueSoftMax = 3;

        private ActionRequestData m_PendingSynthesizedAction = new ActionRequestData();
        private bool m_HasPendingSynthesizedAction;

        public ActionPlayer(ServerCharacter parent)
        {
            m_Parent = parent;
            m_Queue = new List<Action>();
            m_NonBlockingActions = new List<Action>();
        }

        /// <summary>
        /// Perform a sequence of actions.
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            if (!action.ShouldQueue)
            {
                ClearActions();
            }

            if (m_Queue.Count > k_QueueSoftMax && m_Queue[m_Queue.Count - 1].Data.Compare(ref action))
            {
                //this action is redundant with the last action performed. We simply discard it.
                return;
            }

            var newAction = Action.MakeAction(m_Parent, ref action);
            m_Queue.Add(newAction);
            if (m_Queue.Count == 1) { StartAction(); }
        }

        public void ClearActions()
        {
            if (m_Queue.Count > 0)
            {
                m_Queue[0].Cancel();
            }

            //only the first element of the queue is running, so it is the only one that needs to be canceled.
            m_Queue.Clear();
        }

        /// <summary>
        /// If an Action is active, fills out 'data' param and returns true. If no Action is active, returns false
        /// </summary>
        public bool GetActiveActionInfo(out ActionRequestData data)
        {
            if (m_Queue.Count > 0)
            {
                data = m_Queue[0].Data;
                return true;
            }
            else
            {
                data = new ActionRequestData();
                return false;
            }
        }

        /// <summary>
        /// Starts the action at the head of the queue, if any. 
        /// </summary>
        private void StartAction()
        {
            if (m_Queue.Count > 0)
            {
                SynthesizeChaseIfNecessary();

                m_Queue[0].TimeStarted = Time.time;
                bool play = m_Queue[0].Start();
                if (!play)
                {
                    //actions that exited out in the "Start" method will not have their End method called, by design. 
                    AdvanceQueue(false);
                }
            }
        }

        /// <summary>
        /// Synthesizes a Chase Action for the action at the Head of the queue, if necessary (the base action must have a target,
        /// and must have the ShouldClose flag set). This method must not be called when the queue is empty. 
        /// </summary>
        private void SynthesizeChaseIfNecessary()
        {
            Action baseAction = m_Queue[0];

            if (baseAction.Data.ShouldClose && baseAction.Data.TargetIds != null)
            {
                ActionRequestData data = new ActionRequestData
                {
                    ActionTypeEnum = ActionType.GeneralChase,
                    TargetIds = baseAction.Data.TargetIds,
                    Amount = baseAction.Description.Range
                };
                baseAction.Data.ShouldClose = false; //you only get to do this once!
                Action chaseAction = Action.MakeAction(m_Parent, ref data);
                m_Queue.Insert(0, chaseAction);
            }
        }

        /// <summary>
        /// Optionally end the currently playing action, and advance to the next Action that wants to play. 
        /// </summary>
        /// <param name="endRemoved">if true we call End on the removed element.</param>
        private void AdvanceQueue(bool endRemoved)
        {
            if (m_Queue.Count > 0)
            {
                if (endRemoved)
                {
                    m_Queue[0].End();
                    if (m_Queue[0].ChainIntoNewAction(ref m_PendingSynthesizedAction))
                    {
                        Debug.Log($"Going to chain into a new Action: {m_PendingSynthesizedAction.ActionTypeEnum}");
                        m_HasPendingSynthesizedAction = true;
                    }
                }
                m_Queue.RemoveAt(0);
            }

            // now start the new Action! ... unless we now have a pending Action that will supercede it
            if (!m_HasPendingSynthesizedAction || m_PendingSynthesizedAction.ShouldQueue)
            {
                StartAction();
            }
        }

        public void Update()
        {
            if (m_HasPendingSynthesizedAction)
            {
                m_HasPendingSynthesizedAction = false;
                PlayAction(ref m_PendingSynthesizedAction);
            }

            if (m_Queue.Count > 0 && m_Queue[0].ShouldBecomeNonBlocking())
            {
                // the active action is no longer blocking, meaning it should be moved out of the blocking queue and into the
                // non-blocking one. (We use this for e.g. projectile attacks, so the projectiles can keep flying, but
                // the player can enqueue other actions in the meantime.)
                m_NonBlockingActions.Add(m_Queue[0]);
                AdvanceQueue(false);
            }

            // if there's a blocking action, update it
            if (m_Queue.Count > 0)
            {
                if (!UpdateAction(m_Queue[0]))
                {
                    AdvanceQueue(true);
                }
            }

            // if there's non-blocking actions, update them! We do this in reverse-order so we can easily remove expired actions.
            for (int i = m_NonBlockingActions.Count - 1; i >= 0; --i)
            {
                Action runningAction = m_NonBlockingActions[i];
                if (!UpdateAction(runningAction))
                {
                    // it's dead!
                    runningAction.End();
                    m_NonBlockingActions.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Calls a given Action's Update() and decides if the action is still alive.
        /// </summary>
        /// <returns>true if the action is still active, false if it's dead</returns>
        private bool UpdateAction(Action action)
        {
            bool keepGoing = action.Update();
            bool expirable = action.Description.DurationSeconds > 0f; //non-positive value is a sentinel indicating the duration is indefinite. 
            bool timeExpired = expirable && (Time.time - action.TimeStarted) >= action.Description.DurationSeconds;
            return keepGoing && !timeExpired;
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (m_Queue.Count > 0)
            {
                m_Queue[0].OnCollisionEnter(collision);
            }
        }


        /// <summary>
        /// Gives all active Actions a chance to alter a gameplay variable.
        /// </summary>
        /// <param name="enchantmentType">Which gameplay variable is being calculated</param>
        /// <returns>The final ("enchanted") value of the variable</returns>
        public float GetEnchantedValue(Action.EnchantmentType enchantmentType)
        {
            float enchantedValue = Action.GetUnenchantedValue(enchantmentType);
            if (m_Queue.Count > 0)
            {
                m_Queue[0].EnchantValue(enchantmentType, ref enchantedValue);
            }
            foreach (var action in m_NonBlockingActions)
            {
                action.EnchantValue(enchantmentType, ref enchantedValue);
            }
            return enchantedValue;
        }

        /// <summary>
        /// Tells all active Actions that a particular gameplay event happened, such as being hit,
        /// getting healed, dying, etc. Actions can change their behavior as a result.
        /// </summary>
        /// <param name="activityThatOccurred">The type of event that has occurred</param>
        public void OnGameplayActivity(Action.GameplayActivity activityThatOccurred)
        {
            if (m_Queue.Count > 0)
            {
                m_Queue[0].OnGameplayActivity(activityThatOccurred);
            }
            foreach (var action in m_NonBlockingActions)
            {
                action.OnGameplayActivity(activityThatOccurred);
            }
        }

    }
}

