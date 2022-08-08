using System;
using Object = UnityEngine.Object;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    public static class ActionFactory
    {
        /// <summary>
        /// Factory method that creates Actions from their request data.
        /// </summary>
        /// <param name="data">the data to instantiate this skill from. </param>
        /// <returns>the newly created action. </returns>
        public static Action CreateActionFromData(ref ActionRequestData data)
        {
            var actionPrototype = GameDataSource.Instance.GetActionPrototypeByID(data.ActionPrototypeID);

            var ret = Object.Instantiate(actionPrototype);
            ret.Initialize(ref data);
            ret.RuntimePrototypeReference = actionPrototype;
            return ret;
        }

        //todo pool acitons by type and purge when asked to

        //todo convert this static factory method to a DI-compliant instance-based factory (to be able to inject dependencies into actions?)
    }
}
