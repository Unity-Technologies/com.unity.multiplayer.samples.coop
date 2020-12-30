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
        protected ServerCharacter m_parent;

        /// <summary>
        /// The level this action plays back at. e.g. a weak "level 0" melee attack, vs a strong "level 3" melee attack. 
        /// </summary>
        protected int m_level;

        private ActionRequestData m_data;

        /// <summary>
        /// RequestData we were instantiated with. Value should be treated as readonly. 
        /// </summary>
        public ref ActionRequestData Data { get { return ref m_data; } }

        /// <summary>
        /// Data Description for this action. 
        /// </summary>
        public ActionDescription Description
        {
            get
            {
                var list = ActionDescriptionList.LIST[Data.ActionTypeEnum];
                int level = Mathf.Min(m_level, list.Count - 1); //if we don't go up to the requested level, just cap at the max level. 
                return list[level];
            }
        }

        /// <summary>
        /// constructor. The "data" parameter should not be retained after passing in to this method, because we take ownership of its internal memory. 
        /// </summary>
        public Action(ServerCharacter parent, ref ActionRequestData data, int level)
        {
            m_parent = parent;
            m_level = level;
            m_data = data; //do a shallow copy. 
            m_data.Level = level;
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
        /// Factory method that creates Actions from their request data. 
        /// </summary>
        /// <param name="state">the NetworkCharacterState of the character that owns our ActionPlayer</param>
        /// <param name="data">the data to instantiate this skill from. </param>
        /// <param name="level">the level to play the skill at. </param>
        /// <returns>the newly created action. </returns>
        public static Action MakeAction(ServerCharacter parent, ref ActionRequestData data, int level )
        {
            var logic = ActionDescriptionList.LIST[data.ActionTypeEnum][0].Logic;

            switch(logic)
            {
                case ActionLogic.MELEE: return new MeleeAction(parent, ref data, level);
                default: throw new System.NotImplementedException();
            }
        }


    }
}
