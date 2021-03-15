using System.Collections.Generic;
using UnityEngine;
using BlockingMode = BossRoom.ActionDescription.BlockingModeType;

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
        /// To prevent the action queue from growing without bound, we cap its play time to this number of seconds. We can only ever estimate
        /// the time-length of the queue, since actions are allowed to block indefinitely. But this is still a useful estimate that prevents
        /// us from piling up a large number of small actions. 
        /// </summary>
        private const float k_MaxQueueTimeDepth = 1.6f;

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
            if (!action.ShouldQueue && m_Queue.Count > 0 && m_Queue[0].Description.ActionInterruptible )
            {
                ClearActions(false);
            }

            if( GetQueueTimeDepth() >= k_MaxQueueTimeDepth )
            {
                //the queue is too big (in execution seconds) to accommodate any more actions, so this action must be discarded. 
                return;
            }

            var newAction = Action.MakeAction(m_Parent, ref action);
            m_Queue.Add(newAction);
            if (m_Queue.Count == 1) { StartAction(); }
        }

        public void ClearActions(bool cancelNonBlocking)
        {
            if (m_Queue.Count > 0)
            {
                m_Queue[0].Cancel();
            }
            m_Queue.Clear();

            if (cancelNonBlocking)
            {
                foreach (var action in m_NonBlockingActions)
                {
                    action.Cancel();
                }
                m_NonBlockingActions.Clear();
            }
        }

        /// <summary>
        /// If an Action is active, fills out 'data' param and returns true. If no Action is active, returns false.
        /// This only refers to the blocking action! (multiple non-blocking actions can be running in the background, and
        /// this will still return false).
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
        /// Returns how many actions are actively running. This includes all non-blocking actions,
        /// and the one blocking action at the head of the queue (if present).
        /// </summary>
        public int RunningActionCount
        {
            get
            {
                return m_NonBlockingActions.Count + (m_Queue.Count > 0 ? 1 : 0);
            }
        }

        /// <summary>
        /// Starts the action at the head of the queue, if any.
        /// </summary>
        private void StartAction()
        {
            if (m_Queue.Count > 0)
            {
                int index = SynthesizeTargetIfNecessary(0);
                SynthesizeChaseIfNecessary(index);

                m_Queue[0].TimeStarted = Time.time;
                bool play = m_Queue[0].Start();
                if (!play)
                {
                    //actions that exited out in the "Start" method will not have their End method called, by design.
                    AdvanceQueue(false);
                }

                if( m_Queue.Count > 0 && m_Queue[0].Description.ExecTimeSeconds==0 &&
                    m_Queue[0].Description.BlockingMode==ActionDescription.BlockingModeType.OnlyDuringExecTime)
                {
                    //this is a non-blocking action with no exec time. It should never be hanging out at the front of the queue (not even for a frame),
                    //because it could get cleared if a new Action came in in that interval.
                    m_NonBlockingActions.Add(m_Queue[0]);
                    AdvanceQueue(false);
                }
            }
        }

        /// <summary>
        /// Synthesizes a Chase Action for the action at the Head of the queue, if necessary (the base action must have a target,
        /// and must have the ShouldClose flag set). This method must not be called when the queue is empty.
        /// </summary>
        /// <returns>The new index of the Action being operated on.</returns>
        private int SynthesizeChaseIfNecessary(int baseIndex)
        {
            Action baseAction = m_Queue[baseIndex];

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
                m_Queue.Insert(baseIndex, chaseAction);
                return baseIndex + 1;
            }
            return baseIndex;
        }

        /// <summary>
        /// Targeted skills should implicitly set the active target of the character, if not already set.
        /// </summary>
        /// <param name="baseIndex">The new index of the base action in m_Queue</param>
        /// <returns></returns>
        private int SynthesizeTargetIfNecessary(int baseIndex )
        {
            Action baseAction = m_Queue[baseIndex];
            var targets = baseAction.Data.TargetIds;

            if(targets != null && targets.Length == 1 && targets[0] != m_Parent.NetState.TargetId.Value )
            {
                //if this is a targeted skill (with a single requested target), and it is different from our
                //active target, then we synthesize a TargetAction to change  our target over.

                ActionRequestData data = new ActionRequestData
                {
                    ActionTypeEnum = ActionType.GeneralTarget,
                    TargetIds = baseAction.Data.TargetIds
                };

                //this shouldn't run redundantly, because the next time the base Action comes up to play, its Target
                //and the active target in our NetState should match.
                Action targetAction = Action.MakeAction(m_Parent, ref data);
                m_Queue.Insert(baseIndex, targetAction);
                return baseIndex + 1;
            }

            return baseIndex;
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
            var timeElapsed = Time.time - action.TimeStarted;
            bool timeExpired = expirable &&
                timeElapsed >= (action.Description.DurationSeconds + action.Description.CooldownSeconds);
            return keepGoing && !timeExpired;
        }

        /// <summary>
        /// How much time will it take all remaining Actions in the queue to play out? This sums up all the time each Action is blocking,
        /// which is different from each Action's duration. Note that this is an ESTIMATE. An action may block the queue indefinitely if it wishes. 
        /// </summary>
        /// <returns>The total "time depth" of the queue, or how long it would take to play in seconds, if no more actions were added. </returns>
        private float GetQueueTimeDepth()
        {
            if(m_Queue.Count == 0 ) { return 0;  }

            float totalTime = 0;
            foreach( var action in m_Queue )
            {
                var info = action.Description;
                float actionTime =  info.BlockingMode == BlockingMode.OnlyDuringExecTime   ? info.ExecTimeSeconds :
                                    info.BlockingMode == BlockingMode.ExecTimeWithCooldown ? (info.ExecTimeSeconds+info.CooldownSeconds) :
                                    info.BlockingMode == BlockingMode.EntireDuration       ? (info.DurationSeconds + info.CooldownSeconds) :
                                    throw new System.Exception($"Unrecognized blocking mode: {info.BlockingMode}");
                totalTime += actionTime;
            }

            return totalTime - m_Queue[0].TimeRunning;
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
        /// <remarks>
        /// Note that this handles both positive alterations (commonly called "buffs")
        /// AND negative ones ("debuffs").
        /// </remarks>
        /// <param name="buffType">Which gameplay variable is being calculated</param>
        /// <returns>The final ("buffed") value of the variable</returns>
        public float GetBuffedValue(Action.BuffableValue buffType)
        {
            float buffedValue = Action.GetUnbuffedValue(buffType);
            if (m_Queue.Count > 0)
            {
                m_Queue[0].BuffValue(buffType, ref buffedValue);
            }
            foreach (var action in m_NonBlockingActions)
            {
                action.BuffValue(buffType, ref buffedValue);
            }
            return buffedValue;
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


        /// <summary>
        /// Cancels the first instance of the given ActionLogic that is currently running, or all instances if cancelAll is set to true.
        /// Searches actively running actions first, then looks at the head action in the queue.
        /// </summary>
        /// <param name="logic">The ActionLogic to cancel</param>
        /// <param name="cancelAll">If true will cancel all instances; if false will just cancel the first running instance.</param>
        /// <param name="exceptThis">If set, will skip this action (useful for actions canceling other instances of themselves).</param>
        public void CancelRunningActionsByLogic(ActionLogic logic, bool cancelAll, Action exceptThis=null )
        {
            for( int i = m_NonBlockingActions.Count-1; i>=0; --i )
            {
                if( m_NonBlockingActions[i].Description.Logic == logic && m_NonBlockingActions[i] != exceptThis )
                {
                    m_NonBlockingActions[i].Cancel();
                    m_NonBlockingActions.RemoveAt(i);
                    if(!cancelAll) { return;  }
                }
            }

            if( m_Queue.Count > 0 )
            {
                if( m_Queue[0].Description.Logic == logic && m_Queue[0] != exceptThis )
                {
                    m_Queue[0].Cancel();
                    m_Queue.RemoveAt(0);
                }
            }
        }
    }
}

