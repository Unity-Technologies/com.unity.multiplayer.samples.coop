using UnityEngine;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// Abstract base class for playing back the visual feedback of an Action.
    /// </summary>
    public abstract class ActionFX
    {
        protected ClientCharacterVisualization m_ClientParent;
        protected ActionRequestData m_CData;

        /// <summary>
        /// The default hit react animation; several different ActionFXs make use of this.
        /// </summary>
        public const string k_DefaultHitReact = "HitReact1";

        /// <summary>
        /// True if this actionFX began running immediately, prior to getting a confirmation from the server.
        /// </summary>
        public bool c_Anticipated { get; protected set; }

        /// <summary>
        /// Time when this Action was started (from Time.time) in seconds. Set by the ActionPlayer or ActionVisualization.
        /// </summary>
        public float c_TimeStarted { get; set; }

        /// <summary>
        /// How long the Action has been running (since its Start was called)--in seconds, measured via Time.time.
        /// </summary>
        public float c_TimeRunning { get { return (Time.time - c_TimeStarted); } }

        /// <summary>
        /// RequestData we were instantiated with. Value should be treated as readonly.
        /// </summary>
        public ref ActionRequestData c_Data => ref m_CData;

        /// <summary>
        /// Data Description for this action.
        /// </summary>
        public ActionDescription c_Description
        {
            get
            {
                if (!GameDataSource.Instance.ActionDataByType.TryGetValue(c_Data.ActionTypeEnum, out var result))
                {
                    throw new KeyNotFoundException($"Tried to find ActionType {c_Data.ActionTypeEnum} but it was missing from GameDataSource!");
                }

                return result;
            }
        }



        /// <summary>
        /// Starts the ActionFX. Derived classes may return false if they wish to end immediately without their Update being called.
        /// </summary>
        /// <remarks>
        /// Derived class should be sure to call base.OnStart() in their implementation, but note that this resets "Anticipated" to false.
        /// </remarks>
        /// <returns>true to play, false to be immediately cleaned up.</returns>
        public virtual bool OnStartClient()
        {
            c_Anticipated = false; //once you start for real you are no longer an anticipated action.
            c_TimeStarted = UnityEngine.Time.time;
            return true;
        }

        public abstract bool OnUpdateClient();

        /// <summary>
        /// End is always called when the ActionFX finishes playing. This is a good place for derived classes to put
        /// wrap-up logic (perhaps playing the "puff of smoke" that rises when a persistent fire AOE goes away). Derived
        /// classes should aren't required to call base.End(); by default, the method just calls 'Cancel', to handle the
        /// common case where Cancel and End do the same thing.
        /// </summary>
        public virtual void EndClient()
        {
            CancelClient();
        }

        /// <summary>
        /// Cancel is called when an ActionFX is interrupted prematurely. It is kept logically distinct from End to allow
        /// for the possibility that an Action might want to play something different if it is interrupted, rather than
        /// completing. For example, a "ChargeShot" action might want to emit a projectile object in its End method, but
        /// instead play a "Stagger" animation in its Cancel method.
        /// </summary>
        public virtual void CancelClient() { }

        public static ActionFX MakeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent)
        {
            ActionLogic logic = GameDataSource.Instance.ActionDataByType[data.ActionTypeEnum].Logic;
            switch (logic)
            {
                case ActionLogic.Melee: return new MeleeActionFX(ref data, parent);
                case ActionLogic.RangedFXTargeted: return new FXProjectileTargetedActionFX(ref data, parent);
                case ActionLogic.Trample: return new TrampleActionFX(ref data, parent);
                case ActionLogic.AoE: return new AoeActionFX(ref data, parent);
                case ActionLogic.Target: return new TargetActionFX(ref data, parent);

                case ActionLogic.ChargedShield: return new ChargedShieldActionFX(ref data, parent);
                case ActionLogic.ChargedLaunchProjectile: return new ChargedLaunchProjectileActionFX(ref data, parent);

                case ActionLogic.StealthMode: return new StealthModeActionFX(ref data, parent);
                case ActionLogic.DashAttack: return new DashAttackActionFX(ref data, parent);

                case ActionLogic.Stunned:
                case ActionLogic.LaunchProjectile:
                case ActionLogic.Revive:
                case ActionLogic.ImpToss:
                case ActionLogic.Emote: return new AnimationOnlyActionFX(ref data, parent);

                default: throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Should this ActionFX be created anticipatively on the owning client?
        /// </summary>
        /// <param name="parent">The ActionVisualization that would be playing this ActionFX.</param>
        /// <param name="data">The request being sent to the server</param>
        /// <returns>If true ActionVisualization should pre-emptively create the ActionFX on the owning client, before hearing back from the server.</returns>
        public static bool ShouldAnticipateClient(ActionVisualization parent, ref ActionRequestData data)
        {
            if (!parent.Parent.CanPerformActions) { return false; }

            var actionDescription = GameDataSource.Instance.ActionDataByType[data.ActionTypeEnum];

            //for actions with ShouldClose set, we check our range locally. If we are out of range, we shouldn't anticipate, as we will
            //need to execute a ChaseAction (synthesized on the server) prior to actually playing the skill.
            bool isTargetEligible = true;
            if (data.ShouldClose == true)
            {
                ulong targetId = (data.TargetIds != null && data.TargetIds.Length > 0) ? data.TargetIds[0] : 0;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject networkObject))
                {
                    float rangeSquared = actionDescription.Range * actionDescription.Range;
                    isTargetEligible = (networkObject.transform.position - parent.Parent.transform.position).sqrMagnitude < rangeSquared;
                }
            }

            //at present all Actionts anticipate except for the Target action, which runs a single instance on the client and is
            //responsible for action anticipation on its own.
            return isTargetEligible && actionDescription.Logic != ActionLogic.Target;
        }

        /// <summary>
        /// Called when the visualization receives an animation event.
        /// </summary>
        public virtual void OnAnimEventClient(string id) { }

        /// <summary>
        /// Called when this action has finished "charging up". (Which is only meaningful for a
        /// few types of actions -- it is not called for other actions.)
        /// </summary>
        /// <param name="finalChargeUpPercentage"></param>
        public virtual void OnStoppedChargingUpClient(float finalChargeUpPercentage) { }

        /// <summary>
        /// Utility function that instantiates all the graphics in the Spawns list.
        /// If parentToOrigin is true, the new graphics are parented to the origin Transform.
        /// If false, they are positioned/oriented the same way but are not parented.
        /// </summary>
        protected List<SpecialFXGraphic> InstantiateSpecialFXGraphicsClient(Transform origin, bool parentToOrigin)
        {
            var returnList = new List<SpecialFXGraphic>();
            foreach (var prefab in c_Description.Spawns)
            {
                if (!prefab) { continue; } // skip blank entries in our prefab list
                returnList.Add(InstantiateSpecialFXGraphicClient(prefab, origin, parentToOrigin));
            }
            return returnList;
        }

        /// <summary>
        /// Utility function that instantiates one of the graphics from the Spawns list.
        /// If parentToOrigin is true, the new graphics are parented to the origin Transform.
        /// If false, they are positioned/oriented the same way but are not parented.
        /// </summary>
        protected SpecialFXGraphic InstantiateSpecialFXGraphicClient(GameObject prefab, Transform origin, bool parentToOrigin)
        {
            if (prefab.GetComponent<SpecialFXGraphic>() == null)
            {
                throw new System.Exception($"One of the Spawns on action {c_Description.ActionTypeEnum} does not have a SpecialFXGraphic component and can't be instantiated!");
            }
            var graphicsGO = GameObject.Instantiate(prefab, origin.transform.position, origin.transform.rotation, (parentToOrigin ? origin.transform : null));
            return graphicsGO.GetComponent<SpecialFXGraphic>();
        }

        /// <summary>
        /// Called when the action is being "anticipated" on the client. For example, if you are the owner of a tank and you swing your hammer,
        /// you get this call immediately on the client, before the server round-trip.
        /// Overriders should always call the base class in their implementation!
        /// </summary>
        public virtual void AnticipateActionClient()
        {
            c_Anticipated = true;
            c_TimeStarted = UnityEngine.Time.time;

            if (!string.IsNullOrEmpty(c_Description.AnimAnticipation))
            {
                m_ClientParent.OurAnimator.SetTrigger(c_Description.AnimAnticipation);
            }
        }
    }
}


