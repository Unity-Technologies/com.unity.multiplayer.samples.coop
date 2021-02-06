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

        public ActionPlayer(ServerCharacter parent)
        {
            m_Parent = parent;
            m_Queue = new List<Action>();
            m_NonBlockingActions = new List<Action>();
        }

        /// <summary>
        /// Perform a sequence of actions.
        /// </summary>
        public void PlayActions(ActionSequence actions)
        {
            var appendMode = GetAppendability(actions);
            if (appendMode == AppendabilityInfo.Discard)
            {
                // this is effectively the same action as what we're currently running...
                // BUT we already have another one of these enqueued! So we'll silently
                // drop this third copy of the same sequence.
                return;
            }

            if (appendMode == AppendabilityInfo.Clear)
            {
                ClearActions();
            }

            for (int i = 0; i < actions.Count; ++i)
            {
                var new_action = Action.MakeAction(m_Parent, ref actions.Get(i));

                bool wasEmpty = m_Queue.Count == 0;
                m_Queue.Add(new_action);
                if (wasEmpty)
                {
                    AdvanceQueue(false);
                }
            }
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
        /// Optionally end the currently playing action, and advance to the next Action that wants to play. 
        /// </summary>
        /// <param name="expireFirstElement">Pass true to remove the first element and advance to the next element. Pass false to "advance" to the 0th element</param>
        private void AdvanceQueue(bool expireFirstElement)
        {
            if (expireFirstElement && m_Queue.Count > 0)
            {
                m_Queue[0].End();
                m_Queue.RemoveAt(0);
            }

            if (m_Queue.Count > 0)
            {
                m_Queue[0].TimeStarted = Time.time;
                bool play = m_Queue[0].Start();
                if (!play)
                {
                    AdvanceQueue(true);
                }
            }
        }

        public void Update()
        {
            if (m_Queue.Count > 0 &&
                m_Queue[0].Description.BlockingMode == ActionDescription.BlockingModeType.OnlyDuringExecTime &&
                Time.time - m_Queue[0].TimeStarted >= m_Queue[0].Description.ExecTimeSeconds)
            {
                // the active action is no longer blocking, meaning it should be moved out of the blocking queue and into the
                // non-blocking one. (We use this for e.g. projectile attacks, so the projectiles can keep flying, but
                // the player can enqueue other actions in the meantime.)
                m_NonBlockingActions.Add(m_Queue[0]);
                AdvanceQueue(true);
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
            for (int i = m_NonBlockingActions.Count-1; i >= 0; --i)
            {
                Action runningAction = m_NonBlockingActions[i];
                if (!UpdateAction(runningAction))
                {
                    // it's dead!
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

        private enum AppendabilityInfo
        {
            Append,     // this new sequence can be appended to what we're currently running
            Clear,      // this new sequence should replace what we're currently running
            Discard,    // this new sequence COULD be appended, but we already have more than one of these queued up, so throw it out
        }

        /// <summary>
        /// Answers the question: "Can this new sequence of actions be appended to our currently-running queue of actions?"
        /// </summary>
        /// <remarks>
        /// Normally when we get a new sequence, we discard any currently-running sequence. But there's an important special case:
        /// If the new sequence is basically the same as the currently-running sequence, we append this sequence to the end of our list.
        /// We do this to support spam-clicking of basic attacks! When players are clicking like crazy on a monster, they don't want
        /// their previous clicks to be aborted by later clicks... they just want to attack as fast as possible!
        /// </remarks>
        private AppendabilityInfo GetAppendability(ActionSequence sequence)
        {
            if (m_Queue.Count == 0)
            {
                return AppendabilityInfo.Append; // moot
            }

            // We only append sequences if they're basically the same as what we're running. Specifically:
            // - The new sequence is either a singular attack, or a Chase followed by an attack
            // - AND we're currently running that same singular attack (or a chase followed by that attack)
            if (sequence.Count == 0 ||
                sequence.Count > 2 ||
                (sequence.Count == 2 && sequence.Get(0).ActionTypeEnum != ActionType.GeneralChase))
            {
                return AppendabilityInfo.Clear; // don't know how to enqueue this sequence
            }

            ref ActionRequestData actionToCompareWith = ref sequence.Get(sequence.Count - 1);

            int i = 0;
            if (m_Queue[i].Data.ActionTypeEnum == ActionType.GeneralChase)
            {
                ++i; // skip over the currently-running Chase action
            }
            if (m_Queue.Count <= i || m_Queue[i].Data.ActionTypeEnum != actionToCompareWith.ActionTypeEnum)
            {
                return AppendabilityInfo.Clear; // not compatible!
            }

            if (m_Queue.Count > i+1)
            {
                // the new sequence IS compatible, but there's other stuff already enqueued! (Limit 1 extra sequence)
                return AppendabilityInfo.Discard;
            }

            return AppendabilityInfo.Append; // yes! Met all prerequisites for appendability!
        }
    }
}

