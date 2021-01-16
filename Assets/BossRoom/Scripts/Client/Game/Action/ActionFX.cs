using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BossRoom.Visual
{
    /// <summary>
    /// Abstract base class for playing back the visual feedback of an Action. 
    /// </summary>
    public abstract class ActionFX : ActionBase
    {
        protected ClientCharacterVisualization m_Parent;

        public ActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data)
        {
            m_Parent = parent;
        }

        public ActionLogic Logic
        {
            get
            {
                return ActionData.ActionDescriptions[Data.ActionTypeEnum][0].Logic;
            }
        }

        public abstract void Start();

        public abstract bool Update();

        public virtual void End()
        {
            Cancel();
        }

        protected virtual void Cancel() { }

        public static ActionFX MakeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent)
        {
            ActionLogic logic = ActionData.ActionDescriptions[data.ActionTypeEnum][0].Logic;
            switch (logic)
            {
                case ActionLogic.MELEE: return new MeleeActionFX(ref data, parent);
                default: throw new System.NotImplementedException();
            }
        }

        public virtual void OnAnimEvent(string id) { }

    }

}


