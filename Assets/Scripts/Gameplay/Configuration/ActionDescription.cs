using System;
using System.IO;
using System.Linq;
using Unity.Multiplayer.Samples.BossRoom.Actions;
using UnityEditor;
using UnityEngine;
using Action = Unity.Multiplayer.Samples.BossRoom.Actions.Action;

namespace Unity.Multiplayer.Samples.BossRoom
{

    /// <summary>
    /// List of all Actions supported in the game.
    /// </summary>
    public enum ActionType
    {
        None,
        TankBaseAttack,
        ArcherBaseAttack,
        MageBaseAttack,
        RogueBaseAttack,
        ImpBaseAttack,
        ImpBossBaseAttack,
        GeneralChase,
        GeneralRevive,
        DriveArrow,
        Emote1,
        Emote2,
        Emote3,
        Emote4,
        TankTestability,
        TankShieldBuff,
        ImpBossTrampleAttack,
        Stun,
        TankShieldRush,
        GeneralTarget,
        MageHeal,
        ArcherChargedShot,
        RogueStealthMode,
        ArcherVolley,
        RogueDashAttack,
        ImpToss
    }


    /// <summary>
    /// Data description of a single Action, including the information to visualize it (animations etc), and the information
    /// to play it back on the server.
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/ActionDescription", order = 1)]
    public class ActionDescription : ScriptableObject
    {
        [MenuItem("Assets/ConvertToNewAction")]
        private static void ConvertOldActionToNewAction()
        {
            var selectedOldAction = Selection.activeObject as ActionDescription;

            Action action = null;

            switch (selectedOldAction.ActionTypeEnum)
            {
                case ActionType.None:
                    Debug.LogWarning("Can't convert None actiontype to anything");
                    break;
                case ActionType.TankBaseAttack:
                    action = CreateInstance<MeleeAction>();
                    break;
                case ActionType.ArcherBaseAttack:
                    action = CreateInstance<LaunchProjectileAction>();
                    break;
                case ActionType.MageBaseAttack:
                    action = CreateInstance<FXProjectileTargetedAction>();
                    break;
                case ActionType.RogueBaseAttack:
                    action = CreateInstance<MeleeAction>();
                    break;
                case ActionType.ImpBaseAttack:
                    action = CreateInstance<MeleeAction>();
                    break;
                case ActionType.ImpBossBaseAttack:
                    action = CreateInstance<MeleeAction>();
                    break;
                case ActionType.GeneralChase:
                    action = CreateInstance<ChaseAction>();
                    break;
                case ActionType.GeneralRevive:
                    action = CreateInstance<ReviveAction>();
                    break;
                case ActionType.DriveArrow:
                    Debug.LogWarning($"Whoah, what's that action? : {selectedOldAction.name}");
                    break;
                case ActionType.Emote1:
                case ActionType.Emote2:
                case ActionType.Emote3:
                case ActionType.Emote4:
                    action = CreateInstance<EmoteAction>();
                    break;
                case ActionType.TankTestability:
                    action = CreateInstance<AoeAction>();
                    break;
                case ActionType.TankShieldBuff:
                    action = CreateInstance<ChargedShieldAction>();
                    break;
                case ActionType.ImpBossTrampleAttack:
                    action = CreateInstance<TrampleAction>();
                    break;
                case ActionType.Stun:
                    action = CreateInstance<StunnedAction>();
                    break;
                case ActionType.TankShieldRush:
                    action = CreateInstance<ChargedShieldAction>();
                    break;
                case ActionType.GeneralTarget:
                    action = CreateInstance<TargetAction>();
                    break;
                case ActionType.MageHeal:
                    action = CreateInstance<MeleeAction>();
                    break;
                case ActionType.ArcherChargedShot:
                    action = CreateInstance<ChargedLaunchProjectileAction>();
                    break;
                case ActionType.RogueStealthMode:
                    action = CreateInstance<StealthModeAction>();
                    break;
                case ActionType.ArcherVolley:
                    action = CreateInstance<AoeAction>();
                    break;
                case ActionType.RogueDashAttack:
                    action = CreateInstance<DashAttackAction>();
                    break;
                case ActionType.ImpToss:
                    action = CreateInstance<TossAction>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (action == null)
            {
                return;
            }

            action.Config = new ActionConfig();

            action.Config.Amount = selectedOldAction.Amount;
            action.Config.Anim = selectedOldAction.Anim;
            action.Config.Anim2 = selectedOldAction.Anim2;
            action.Config.Description = selectedOldAction.Description;
            action.Config.Icon = selectedOldAction.Icon;
            action.Config.Logic = selectedOldAction.Logic;
            action.Config.Projectiles = selectedOldAction.Projectiles.ToArray();
            action.Config.Radius = selectedOldAction.Radius;
            action.Config.Range = selectedOldAction.Range;
            action.Config.Spawns = selectedOldAction.Spawns.ToArray();
            action.Config.ActionInput = selectedOldAction.ActionInput;
            action.Config.ActionInterruptible = selectedOldAction.ActionInterruptible;
            action.Config.AnimAnticipation = selectedOldAction.AnimAnticipation;
            action.Config.BlockingMode = selectedOldAction.BlockingMode;
            action.Config.DisplayedName = selectedOldAction.DisplayedName;
            action.Config.DurationSeconds = selectedOldAction.DurationSeconds;
            action.Config.IsFriendly = selectedOldAction.IsFriendly;
            action.Config.KnockbackDuration = selectedOldAction.KnockbackDuration;
            action.Config.KnockbackSpeed = selectedOldAction.KnockbackSpeed;
            action.Config.ManaCost = selectedOldAction.ManaCost;
            action.Config.MoveSpeed = selectedOldAction.MoveSpeed;
            action.Config.ReactAnim = selectedOldAction.ReactAnim;
            action.Config.SplashDamage = selectedOldAction.SplashDamage;
            action.Config.EffectDurationSeconds = selectedOldAction.EffectDurationSeconds;
            action.Config.ExecTimeSeconds = selectedOldAction.ExecTimeSeconds;
            action.Config.OtherAnimatorVariable = selectedOldAction.OtherAnimatorVariable;
            action.Config.ReuseTimeSeconds = selectedOldAction.ReuseTimeSeconds;

            var oldPath = AssetDatabase.GetAssetPath(selectedOldAction);

            var newPath = $"{Path.GetDirectoryName(oldPath)}/new_{selectedOldAction.name}.asset";

            AssetDatabase.CreateAsset(action, newPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = action;
        }

        // Note that we pass the same path, and also pass "true" to the second argument.
        [MenuItem("Assets/ConvertToNewAction", true)]
        private static bool ValidateIfOldActionSelected()
        {
            return Selection.activeObject is ActionDescription;
        }


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

    }
}

