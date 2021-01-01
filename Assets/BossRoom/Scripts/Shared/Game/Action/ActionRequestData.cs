using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// List of all Actions supported in the game. 
    /// </summary>
    public enum ActionType
    {
        TANK_BASEATTACK,
        ARCHER_BASEATTACK,
        GENERAL_CHASE,
    }


    /// <summary>
    /// List of all Types of Actions. There is a many-to-one mapping of Actions to ActionLogics. 
    /// </summary>
    public enum ActionLogic
    {
        MELEE,
        RANGED,
        RANGEDTARGETED,
        CHASE,

        //O__O adding a new ActionLogic branch? Update Action.MakeAction!
    }

    /// <summary>
    /// FIXME: [GOMPS-99] This will be turned into a ScriptableObject. 
    /// </summary>
    public class ActionDescription
    {
        /// <summary>
        /// ActionLogic that drives this Action. This corresponds to the actual block of code that executes it. 
        /// </summary>
        public ActionLogic Logic;

        /// <summary>
        /// Could be damage, could be healing, or other things. This is a base, nominal value that will get modified
        /// by game logic when the action takes effect. 
        /// </summary>
        public int Amount;

        /// <summary>
        /// How much it consts in Mana to play this Action. 
        /// </summary>
        public int ManaCost;

        /// <summary>
        /// How how the Action performer can be from the Target, or how far the action can go (for an untargeted action like a bowshot). 
        /// </summary>
        public float Range;

        /// <summary>
        /// Duration in seconds that this Action takes to play. 
        /// </summary>
        public float Duration_s;

        /// <summary>
        /// How long the effect this Action leaves behind will last, in seconds. 
        /// </summary>
        public float EffectDuration_s;

        /// <summary>
        /// The primary Animation action that gets played when visualizing this Action. 
        /// </summary>
        public string Anim;
    }

    /// <summary>
    /// metadata about each kind of ActionLogic. This basically just informs us what fields to serialize for each kind of ActionLogic. 
    /// </summary>
    public class ActionLogicInfo
    {
        public bool HasPosition;
        public bool HasDirection;
        public bool HasTarget;
        public bool HasAmount;
    }

    /// <summary>
    /// FIXME [GOMPS-99]: this list will be turned into a collection of Scriptable Objects. 
    /// Question: Do we want to show how to do skill levels, as I am doing here?
    /// </summary>
    public class ActionData
    {
        public static Dictionary<ActionLogic, ActionLogicInfo> LogicInfos = new Dictionary<ActionLogic, ActionLogicInfo>
        {
            {ActionLogic.MELEE, new ActionLogicInfo{} },
            {ActionLogic.RANGED, new ActionLogicInfo{HasDirection=true} },
            {ActionLogic.RANGEDTARGETED, new ActionLogicInfo{HasTarget=true} },
            {ActionLogic.CHASE, new ActionLogicInfo{HasTarget=true, HasAmount=true} },
        };

        public static Dictionary<ActionType, List<ActionDescription>> ActionDescriptions = new Dictionary<ActionType, List<ActionDescription>>
        {
            { ActionType.TANK_BASEATTACK , new List<ActionDescription>
                {
                    {new ActionDescription{Logic=ActionLogic.MELEE, Amount=10, ManaCost=2, Duration_s=0.5f, Range=4f, Anim="Todo" } },  //level 1
                    {new ActionDescription{Logic=ActionLogic.MELEE, Amount=15, ManaCost=2, Duration_s=0.5f, Range=4f, Anim="Todo" } },  //level 2
                    {new ActionDescription{Logic=ActionLogic.MELEE, Amount=20, ManaCost=2, Duration_s=0.5f, Range=4f, Anim="Todo" } },  //level 3
                }
            },

            { ActionType.ARCHER_BASEATTACK, new List<ActionDescription>
                {
                    {new ActionDescription{Logic=ActionLogic.RANGED, Amount=7,  ManaCost=2, Duration_s=0.5f, Range=12f, Anim="Todo" } }, //Level 1
                    {new ActionDescription{Logic=ActionLogic.RANGED, Amount=12, ManaCost=2, Duration_s=0.5f, Range=15f, Anim="Todo" } }, //Level 2
                    {new ActionDescription{Logic=ActionLogic.RANGED, Amount=15, ManaCost=2, Duration_s=0.5f, Range=18f, Anim="Todo" } }, //Level 3
                }
            },

            { ActionType.GENERAL_CHASE, new List<ActionDescription> 
                {
                    {new ActionDescription{Logic=ActionLogic.CHASE } }
                } 
            }



                
        };
    }



    /// <summary>
    /// Comprehensive class that contains information needed to play back any action on the server. This is what gets sent client->server when
    /// the Action gets played, and also what gets sent server->client to broadcast the action event. Note that the OUTCOMES of the action effect
    /// don't ride along with this object when it is broadcast to clients; that information is sync'd separately, usually by NetworkedVars.
    /// </summary>
    public struct ActionRequestData
    {
        public ActionType ActionTypeEnum;      //the action to play. 
        public Vector3 Position;           //center position of skill, e.g. "ground zero" of a fireball skill. 
        public Vector3 Direction;          //direction of skill, if not inferrable from the character's current facing. 
        public ulong[] TargetIds;          //networkIds of targets, or null if untargeted. 
        public int Level;                  //what level the Action plays at (server->client only). Levels are 0-based, with 0 being weakest. 
        public float Amount;               //can mean different things depending on the Action. For a ChaseAction, it will be target range the ChaseAction is trying to achieve.
        public bool ShouldQueue;           //if true, this action should queue. If false, it should clear all current actions and play immediately. 

        //O__O Hey, are you adding something? Be sure to update ActionLogicInfo and NetworkCharacterState.SerializeAction, RecvDoAction as well. 
    }

}

