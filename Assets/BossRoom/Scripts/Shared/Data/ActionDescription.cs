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

        [Tooltip("How much it consts in Mana to play this Action")]
        public int ManaCost;

        [Tooltip("How how the Action performer can be from the Target, or how far the action can go (for an untargeted action like a bowshot")]
        public float Range;

        [Tooltip("Duration in seconds that this Action takes to play")]
        public float Duration_s;

        [Tooltip("Time when the Action should do its \"main thing\" (e.g. when a melee attack should apply damage")]
        public float ExecTime_s;

        [Tooltip("How long the effect this Action leaves behind will last, in seconds")]
        public float EffectDuration_s;

        [Tooltip("The primary Animation action that gets played when visualizing this Action")]
        public string Anim;

        [Tooltip("The radius of effect for this action. Default is 0 if not needed")]
        public float Radius;
    }
}

