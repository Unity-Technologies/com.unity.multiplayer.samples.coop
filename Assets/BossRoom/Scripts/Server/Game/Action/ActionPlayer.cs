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

        public ActionPlayer(ServerCharacter parent)
        {
            m_Parent = parent;
            m_Queue = new List<Action>();
        }

        public void PlayAction(ref ActionRequestData data)
        {
            if (!data.ShouldQueue)
            {
                ClearActions();
            }

            var new_action = Action.MakeAction(m_Parent, ref data);

            bool wasEmpty = m_Queue.Count == 0;
            m_Queue.Add(new_action);
            if (wasEmpty)
            {
                AdvanceQueue(false);
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
            if (m_Queue.Count > 0)
            {
                Action runningAction = m_Queue[0]; //action at the front of the queue is the one that is actively running. 
                bool keepGoing = runningAction.Update();
                bool expirable = runningAction.Description.Duration_s > 0f; //non-positive value is a sentinel indicating the duration is indefinite. 
                bool timeExpired = expirable && (Time.time - runningAction.TimeStarted) >= runningAction.Description.Duration_s;
                if (!keepGoing || timeExpired)
                {
                    AdvanceQueue(true);
                }
            }
        }


    }
}

