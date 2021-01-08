using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Class responsible for playing back action inputs from user. 
    /// </summary>
    public class ActionPlayer
    {
        private ServerCharacter m_parent;

        private List<Action> m_queue;

        public ActionPlayer(ServerCharacter parent )
        {
            m_parent = parent;
            m_queue = new List<Action>();
        }

        public void PlayAction(ref ActionRequestData data )
        {
            //sanity check that prevents dead or fainted characters from executing actions
            if (m_parent.NetState.NetworkLifeState.Value != LifeState.ALIVE)
            {
                return;
            }
            
            if( !data.ShouldQueue )
            {
                ClearActions();
            }

            int level = 0; //todo, get this from parent's networked vars, maybe. 
            var new_action = Action.MakeAction(m_parent, ref data, level);

            bool was_empty = m_queue.Count == 0;
            m_queue.Add(new_action);
            if( was_empty )
            {
                AdvanceQueue(false);
            }
        }

        public void ClearActions()
        {
            if( m_queue.Count > 0 )
            {
                m_queue[0].Cancel();
            }

            //only the first element of the queue is running, so it is the only one that needs to be canceled. 
            m_queue.Clear();
        }

        /// <summary>
        /// If an Action is active, fills out 'data' param and returns true. If no Action is active, returns false
        /// </summary>
        public bool GetActiveActionInfo(out ActionRequestData data)
        {
            if (m_queue.Count > 0)
            {
                data = m_queue[ 0 ].Data;
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
            if( expireFirstElement && m_queue.Count > 0 )
            {
                m_queue.RemoveAt(0);
            }

            if( m_queue.Count > 0 )
            {
                m_queue[0].TimeStarted = Time.time;
                bool play = m_queue[0].Start();
                if( !play )
                {
                    AdvanceQueue(true);
                }
            }
        }

        public void Update()
        {
            if( this.m_queue.Count > 0 )
            {
                Action runningAction = m_queue[0]; //action at the front of the queue is the one that is actively running. 
                bool keepGoing = runningAction.Update();
                bool expirable = runningAction.Description.Duration_s > 0f; //non-positive value is a sentinel indicating the duration is indefinite. 
                bool timeExpired = expirable && (Time.time - runningAction.TimeStarted) >= runningAction.Description.Duration_s;
                if ( !keepGoing || timeExpired )
                {
                    AdvanceQueue(true);
                }
            }
        }


    }
}

