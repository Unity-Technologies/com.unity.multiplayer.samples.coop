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

        [Tooltip("How how the Action performer can be from the Target, or how far the action can go (for an untargeted action like a bowshot")]
        public float Range;

        [Tooltip("Duration in seconds that this Action takes to play")]
        public float DurationSeconds;

        [Tooltip("Time when the Action should do its \"main thing\" (e.g. when a melee attack should apply damage")]
        public float ExecTimeSeconds;

        [Tooltip("How long the effect this Action leaves behind will last, in seconds")]
        public float EffectDurationSeconds;

        [Tooltip("The primary Animation action that gets played when visualizing this Action")]
        public string Anim;

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
        [Tooltip("If this action spawns GameObjects, list their prefabs here")]
        public GameObject[] Spawns;

        [Tooltip("If this Action spawns a projectile, how fast should that projectile move? (meters/second)")]
        public float ProjectileSpeed_m_s;

    }
}

