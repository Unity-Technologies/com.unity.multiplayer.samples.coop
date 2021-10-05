using UnityEngine;
using BlockingMode = Unity.Multiplayer.Samples.BossRoom.ActionDescription.BlockingModeType;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// The abstract parent class that all Actions derive from.
    /// </summary>
    /// <remarks>
    /// The Action System is a generalized mechanism for Characters to "do stuff" in a networked way. Actions
    /// include everything from your basic character attack, to a fancy skill like the Archer's Volley Shot, but also
    /// include more mundane things like pulling a lever.
    /// For every ActionLogic enum, there will be one specialization of this class.
    /// There is only ever one active Action (also called the "blocking" action) at a time on a character, but multiple
    /// Actions may exist at once, with subsequent Actions pending behind the currently active one, and possibly
    /// "non-blocking" actions running in the background. See ActionPlayer.cs
    ///
    /// The flow for Actions is:
    /// Initially: Start()
    /// Every frame: ShouldBecomeNonBlocking() (only if Action is blocking), then Update()
    /// On shutdown: End() or Cancel()
    /// After shutdown: ChainIntoNewAction()    (only if Action was blocking, and only if End() was called, not Cancel())
    ///
    /// Note also that if Start() returns false, no other functions are called on the Action, not even End().
    /// </remarks>
    public abstract class Action : ActionBase
    {
        protected ServerCharacter m_Parent;

        /// <summary>
        /// constructor. The "data" parameter should not be retained after passing in to this method, because we take ownership of its internal memory.
        /// </summary>
        public Action(ServerCharacter parent, ref ActionRequestData data) : base(ref data)
        {
            m_Parent = parent;
            m_Data = data; //do a shallow copy.
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
        /// Called each frame (before Update()) for the active ("blocking") Action, asking if it should become a background Action.
        /// </summary>
        /// <returns>true to become a non-blocking Action, false to remain a blocking Action</returns>
        public virtual bool ShouldBecomeNonBlocking()
        {
            return Description.BlockingMode == BlockingMode.OnlyDuringExecTime ?  TimeRunning >= Description.ExecTimeSeconds : false;
        }

        /// <summary>
        /// Called when the Action ends naturally. By default just calls Cancel()
        /// </summary>
        public virtual void End()
        {
            Cancel();
        }

        /// <summary>
        /// This will get called when the Action gets canceled. The Action should clean up any ongoing effects at this point.
        /// (e.g. an Action that involves moving should cancel the current active move).
        /// </summary>
        public virtual void Cancel() { }

        /// <summary>
        /// Called *AFTER* End(). At this point, the Action has ended, meaning its Update() etc. functions will never be
        /// called again. If the Action wants to immediately segue into a different Action, it can do so here. The new
        /// Action will take effect in the next Update().
        ///
        /// Note that this is not called on prematurely cancelled Actions, only on ones that have their End() called.
        /// </summary>
        /// <param name="newAction">the new Action to immediately transition to</param>
        /// <returns>true if there's a new action, false otherwise</returns>
        public virtual bool ChainIntoNewAction(ref ActionRequestData newAction) { return false;  }

        /// <summary>
        /// Called on the active ("blocking") Action when this character collides with another.
        /// </summary>
        /// <param name="collision"></param>
        public virtual void OnCollisionEnter(Collision collision) { }

        public enum BuffableValue
        {
            PercentHealingReceived, // unbuffed value is 1.0. Reducing to 0 would mean "no healing". 2 would mean "double healing"
            PercentDamageReceived,  // unbuffed value is 1.0. Reducing to 0 would mean "no damage". 2 would mean "double damage"
            ChanceToStunTramplers,  // unbuffed value is 0. If > 0, is the chance that someone trampling this character becomes stunned
        }

        /// <summary>
        /// Called on all active Actions to give them a chance to alter the outcome of a gameplay calculation. Note
        /// that this is used for both "buffs" (positive gameplay benefits) and "debuffs" (gameplay penalties).
        /// </summary>
        /// <remarks>
        /// In a more complex game with lots of buffs and debuffs, this function might be replaced by a separate
        /// BuffRegistry component. This would let you add fancier features, such as defining which effects
        /// "stack" with other ones, and could provide a UI that lists which are affecting each character
        /// and for how long.
        /// </remarks>
        /// <param name="buffType">Which gameplay variable being calculated</param>
        /// <param name="orgValue">The original ("un-buffed") value</param>
        /// <param name="buffedValue">The final ("buffed") value</param>
        public virtual void BuffValue(BuffableValue buffType, ref float buffedValue) { }

        /// <summary>
        /// Static utility function that returns the default ("un-buffed") value for a BuffableValue.
        /// (This just ensures that there's one place for all these constants.)
        /// </summary>
        public static float GetUnbuffedValue(Action.BuffableValue buffType)
        {
            switch (buffType)
            {
                case BuffableValue.PercentDamageReceived: return 1;
                case BuffableValue.PercentHealingReceived: return 1;
                case BuffableValue.ChanceToStunTramplers: return 0;
                default: throw new System.Exception($"Unknown buff type {buffType}");
            }
        }

        public enum GameplayActivity
        {
            AttackedByEnemy,
            Healed,
            StoppedChargingUp,
            UsingAttackAction, // called immediately before we perform the attack Action
        }

        /// <summary>
        /// Called on active Actions to let them know when a notable gameplay event happens.
        /// </summary>
        /// <remarks>
        /// When a GameplayActivity of AttackedByEnemy or Healed happens, OnGameplayAction() is called BEFORE BuffValue() is called.
        /// </remarks>
        /// <param name="actionType"></param>
        public virtual void OnGameplayActivity(GameplayActivity activityType) { }

        /// <summary>
        /// Factory method that creates Actions from their request data.
        /// </summary>
        /// <param name="parent">The component that owns the ActionPlayer this action is running on. </param>
        /// <param name="data">the data to instantiate this skill from. </param>
        /// <returns>the newly created action. </returns>
        public static Action MakeAction(ServerCharacter parent, ref ActionRequestData data)
        {
            if (!GameDataSource.Instance.ActionDataByType.TryGetValue(data.ActionTypeEnum, out var actionDesc))
            {
                throw new System.Exception($"Trying to create Action {data.ActionTypeEnum} but it isn't defined on the GameDataSource!");
            }

            var logic = actionDesc.Logic;

            switch (logic)
            {
                case ActionLogic.Melee: return new MeleeAction(parent, ref data);
                case ActionLogic.AoE: return new AoeAction(parent, ref data);
                case ActionLogic.Chase: return new ChaseAction(parent, ref data);
                case ActionLogic.Revive: return new ReviveAction(parent, ref data);
                case ActionLogic.RangedFXTargeted: return new FXProjectileTargetedAction(parent, ref data);
                case ActionLogic.LaunchProjectile: return new LaunchProjectileAction(parent, ref data);
                case ActionLogic.Emote: return new EmoteAction(parent, ref data);
                case ActionLogic.Trample: return new TrampleAction(parent, ref data);
                case ActionLogic.ChargedShield: return new ChargedShieldAction(parent, ref data);
                case ActionLogic.Stunned: return new StunnedAction(parent, ref data);
                case ActionLogic.Target: return new TargetAction(parent, ref data);
                case ActionLogic.ChargedLaunchProjectile: return new ChargedLaunchProjectileAction(parent, ref data);
                case ActionLogic.StealthMode: return new StealthModeAction(parent, ref data);
                case ActionLogic.DashAttack: return new DashAttackAction(parent, ref data);
                default: throw new System.NotImplementedException();
            }
        }


    }
}
