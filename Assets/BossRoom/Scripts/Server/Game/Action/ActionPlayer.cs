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
        private float m_actionStarted_s = 0f; 

        public ActionPlayer(ServerCharacter parent )
        {
            m_parent = parent;
            m_queue = new List<Action>();
        }

        public void PlayAction(ref ActionRequestData data )
        {
            int level = 0; //todo, get this from parent's networked vars, maybe. 
            var new_action = Action.MakeAction(m_parent, ref data, level);

            bool was_empty = m_queue.Count == 0;
            m_queue.Add(new_action);
            if( was_empty )
            {
                AdvanceQueue(false);
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
                m_actionStarted_s = Time.time;
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
                bool keepgoing = this.m_queue[0].Update();
                bool time_expired = (Time.time - this.m_actionStarted_s) >= this.m_queue[0].Description.Duration_s;
                if ( !keepgoing || time_expired )
                {
                    this.AdvanceQueue(true);
                }
            }
        }


    }
}

