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
        /// Called when the Action ends naturally. By default just calls logic in "cancel", but derived classes can do
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
        /// Factory method that creates Actions from their request data.
        /// </summary>
        /// <param name="parent">The component that owns the ActionPlayer this action is running on. </param>
        /// <param name="data">the data to instantiate this skill from. </param>
        /// <returns>the newly created action. </returns>
        public static Action MakeAction(ServerCharacter parent, ref ActionRequestData data)
        {
            ActionDescription actionDesc;
            if (!GameDataSource.Instance.ActionDataByType.TryGetValue(data.ActionTypeEnum, out actionDesc))
            {
                throw new System.Exception($"Trying to create Action {data.ActionTypeEnum} but it isn't defined on the GameDataSource!");
            }

            var logic = actionDesc.Logic;

            switch (logic)
            {
                case ActionLogic.Melee: return new MeleeAction(parent, ref data);
                case ActionLogic.Chase: return new ChaseAction(parent, ref data);
                case ActionLogic.Revive: return new ReviveAction(parent, ref data);
                case ActionLogic.RangedFXTargeted: return new FXProjectileTargetedAction(parent, ref data);
                case ActionLogic.LaunchProjectile: return new LaunchProjectileAction(parent, ref data);
                case ActionLogic.Emote: return new EmoteAction(parent, ref data);
                default: throw new System.NotImplementedException();
            }
        }


    }
}
