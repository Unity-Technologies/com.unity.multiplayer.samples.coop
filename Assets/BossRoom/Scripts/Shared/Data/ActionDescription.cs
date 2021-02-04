using UnityEngine;

namespace BossRoom
{

    /// <summary>
    /// An enum representing a direction relative to the entity playing the action. It can represent where an attack is beginning to detect the first target hit,
    /// or a direction to have an attack move towards.
    /// </summary>
    public enum HitDirection
    {
        NoDirection,
        Left,
        Right,
    }

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

        [Tooltip("A designation for where the Action starts playing relative to the performer. This can be used for having certain attacks organize who should get hit first in the attack")]
        public HitDirection HitStartDirection;
    }


}

