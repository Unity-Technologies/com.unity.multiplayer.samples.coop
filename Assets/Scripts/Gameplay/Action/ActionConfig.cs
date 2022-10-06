using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    [Serializable]
    public class ActionConfig
    {
        [Tooltip("ActionLogic that drives this Action. This corresponds to the actual block of code that executes it.")]
        public ActionLogic Logic;

        [Tooltip("Could be damage, could be healing, or other things. This is a base, nominal value that will get modified by game logic when the action takes effect")]
        public int Amount;

        [Tooltip("How much it costs in Mana to play this Action")]
        public int ManaCost;

        [Tooltip("How far the Action performer can be from the Target")]
        public float Range;

        [Tooltip("Duration in seconds that this Action takes to play")]
        public float DurationSeconds;

        [Tooltip("Time when the Action should do its \"main thing\" (e.g. when a melee attack should apply damage")]
        public float ExecTimeSeconds;

        [Tooltip("How long the effect this Action leaves behind will last, in seconds")]
        public float EffectDurationSeconds;

        [Tooltip("After this Action is successfully started, the server will discard any attempts to perform it again until this amount of time has elapsed.")]
        public float ReuseTimeSeconds;

        [Tooltip("The Anticipation Animation trigger that gets raised when user starts using this Action, but while the server confirmation hasn't returned")]
        public string AnimAnticipation;

        [Tooltip("The primary Animation trigger that gets raised when visualizing this Action")]
        public string Anim;

        [Tooltip("The auxiliary Animation trigger for this Action (e.g. to end an animation loop)")]
        public string Anim2;

        [Tooltip("The reaction anim to play in response to being hit by this skill")]
        public string ReactAnim;

        [Tooltip("The name of an animator variable used by this action")]
        public string OtherAnimatorVariable;

        [Tooltip("For Actions that can hit multiple enemies, this determines how much damage is done to non-primary targets")]
        public int SplashDamage;

        [Tooltip("For actions that change your speed (e.g. Trample), what speed do we have?")]
        public float MoveSpeed;

        [Tooltip("For actions that cause a knockback, how potent is the knockback force?")]
        public float KnockbackSpeed;

        [Tooltip("For actions that cause a knockback, how long does it apply force to the target?")]
        public float KnockbackDuration;

        [Tooltip("The radius of effect for this action. Default is 0 if not needed")]
        public float Radius;

        [Tooltip("Prefab to spawn that will manage this action's input")]
        public BaseActionInput ActionInput;

        [Tooltip("Is this Action interruptible by other action-plays or by movement? (Implicitly stops movement when action starts.) Generally, actions with short exec times should not be interruptible in this way.")]
        public bool ActionInterruptible;

        [Tooltip("This action is interrupted if any of the following actions is requested")]
        public List<Action> IsInterruptableBy;

        [Tooltip("Indicates how long this action blocks other actions from happening: during the execution stage, or for as long as it runs?")]
        public BlockingModeType BlockingMode;

        [Tooltip("If this Action spawns a projectile, describes it. (\"Charged\" projectiles can list multiple possible shots, ordered from weakest to strongest)")]
        public ProjectileInfo[] Projectiles;

        [Tooltip("If this action spawns miscellaneous GameObjects, list their prefabs here (but not projectiles -- those are separate, see above!)")]
        public GameObject[] Spawns;

        [Tooltip("If true, this action affects friendly targets, if false Unfriendly. Not all ActionLogics use this parameter.")]
        public bool IsFriendly;

        [Header("In-game description info (Only used for player abilities!)")]
        [Tooltip("If this Action describes a player ability, this is the ability's iconic representation")]
        public Sprite Icon;

        [Tooltip("If this Action describes a player ability, this is the name we show for the ability")]
        public string DisplayedName;

        [Tooltip("If this Action describes a player ability, this is the tooltip description we show for the ability")]
        [Multiline]
        public string Description;

        public bool CanBeInterruptedBy(ActionID actionActionID)
        {
            foreach (var action in IsInterruptableBy)
            {
                if (action.ActionID == actionActionID)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
