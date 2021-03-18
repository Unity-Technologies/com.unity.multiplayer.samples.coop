using MLAPI.Serialization;
using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Data description of a single Action, including the information to visualize it (animations etc), and the information
    /// to play it back on the server.
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/ActionDescription", order = 1)]
    public class ActionDescription : ScriptableObject
    {
        [Tooltip("The ActionType this is the data for. An enum that represents the specific action, compared to ActionLogic, where multiple Actions can share the same Logic")]
        public ActionType ActionTypeEnum;

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

        [Tooltip("Duration in seconds that this Action takes to cooldown, after DurationSeconds has elapsed.")]
        public float CooldownSeconds;

        [Tooltip("Time when the Action should do its \"main thing\" (e.g. when a melee attack should apply damage")]
        public float ExecTimeSeconds;

        [Tooltip("How long the effect this Action leaves behind will last, in seconds")]
        public float EffectDurationSeconds;

        [Tooltip("The primary Animation trigger that gets raised when visualizing this Action")]
        public string Anim;

        [Tooltip("The auxiliary Animation trigger for this Action (e.g. to end an animation loop)")]
        public string Anim2;

        [Tooltip("The reaction anim to play in response to being hit by this skill")]
        public string ReactAnim;

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

        [Tooltip("Is this Action interruptible by other action plays. Generally, actions with short exec times should not be interruptible in this way.")]
        public bool ActionInterruptible;

        [System.Serializable]
        public enum BlockingModeType
        {
            EntireDuration,
            OnlyDuringExecTime,
            ExecTimeWithCooldown,
        }
        [Tooltip("Indicates how long this action blocks other actions from happening: during the execution stage, or for as long as it runs?")]
        public BlockingModeType BlockingMode;

        [Serializable]
        public struct ProjectileInfo
        {
            [Tooltip("Prefab used for the projectile")]
            public GameObject ProjectilePrefab;
            [Tooltip("Projectile's speed in meters/second")]
            public float Speed_m_s;
            [Tooltip("Maximum range of the Projectile")]
            public float Range;
            [Tooltip("Damage of the Projectile on hit")]
            public int Damage;
            [Tooltip("Max number of enemies this projectile can hit before disappearing")]
            public int MaxVictims;
        }

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

    }
}

