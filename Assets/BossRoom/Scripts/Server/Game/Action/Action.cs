using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// The abstract parent class that all Actions derive from. 
    /// </summary>
    /// <remarks>
    /// The Action System is a generalized mechanism for Characters to "do stuff" in a networked way. Actions
    /// include everything from your basic character attack, to a fancy skill like the Archer's Volley Shot, but also 
    /// include more mundane things like pulling a lever.
    /// For every ActionLogic enum, there will be one specialization of this class. 
    /// There is only ever one active Action at a time on a character, but multiple Actions may exist at once, with subsequent Actions
    /// pending behind the currently playing one. See ActionPlayer.cs
    /// </remarks>
    public abstract class Action
    {
        protected ServerCharacter m_Parent;

        /// <summary>
        /// The level this action plays back at. e.g. a weak "level 0" melee attack, vs a strong "level 3" melee attack. 
        /// </summary>
        protected int m_Level;

        protected ActionRequestData m_Data;

        /// <summary>
        /// Time when this Action was started (from Time.time) in seconds. Set by the ActionPlayer. 
        /// </summary>
        public float TimeStarted { get; set; }

        /// <summary>
        /// RequestData we were instantiated with. Value should be treated as readonly. 
        /// </summary>
        public ref ActionRequestData Data { get { return ref m_Data; } }

        /// <summary>
        /// Data Description for this action. 
        /// </summary>
        public ActionDescription Description
        {
            get
            {
                var list = ActionData.ActionDescriptions[Data.ActionTypeEnum];
                int level = Mathf.Min(m_Level, list.Count - 1); //if we don't go up to the requested level, just cap at the max level. 
                return list[level];
            }
        }

        /// <summary>
        /// constructor. The "data" parameter should not be retained after passing in to this method, because we take ownership of its internal memory. 
        /// </summary>
        public Action(ServerCharacter parent, ref ActionRequestData data, int level)
        {
            m_Parent = parent;
            m_Level = level;
            m_Data = data; //do a shallow copy. 
            m_Data.Level = level;
        }

        /// <summary>
        /// Called when the Action starts actually playing (which may be after it is created, because of queueing). 
        /// </summary>
        /// <returns>false if the action decided it doesn't want to run after all, true otherwise. </returns>
        public abstract bool Start();

        /// <summary>
        /// Called each frame while the action is running. 
        /// </summary>
        /// <returns>true to keep running, false to stop. The Action will stop by default when its duration expires, if it has a duration set. </returns>
        public abstract bool Update();

        /// <summary>
        /// This will get called when the Action gets canceled. The Action should clean up any ongoing effects at this point. 
        /// (e.g. an Action that involves moving should cancel the current active move). 
        /// </summary>
        public virtual void Cancel() { }


        /// <summary>
        /// Factory method that creates Actions from their request data. 
        /// </summary>
        /// <param name="state">the NetworkCharacterState of the character that owns our ActionPlayer</param>
        /// <param name="data">the data to instantiate this skill from. </param>
        /// <param name="level">the level to play the skill at. </param>
        /// <returns>the newly created action. </returns>
        public static Action MakeAction(ServerCharacter parent, ref ActionRequestData data, int level )
        {
            var logic = ActionData.ActionDescriptions[data.ActionTypeEnum][0].Logic;

            switch(logic)
            {
                case ActionLogic.MELEE: return new MeleeAction(parent, ref data, level);
                case ActionLogic.CHASE: return new ChaseAction(parent, ref data, level);
                default: throw new System.NotImplementedException();
            }
        }


    }
}
