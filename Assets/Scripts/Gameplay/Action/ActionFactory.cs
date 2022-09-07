using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Unity.BossRoom.Gameplay.Actions
{
    public static class ActionFactory
    {
        private static Dictionary<ActionID, ObjectPool<Action>> s_ActionPools = new Dictionary<ActionID, ObjectPool<Action>>();

        private static ObjectPool<Action> GetActionPool(ActionID actionID)
        {
            if (!s_ActionPools.TryGetValue(actionID, out var actionPool))
            {
                actionPool = new ObjectPool<Action>(
                    createFunc: () => Object.Instantiate(GameDataSource.Instance.GetActionPrototypeByID(actionID)),
                    actionOnRelease: action => action.Reset(),
                    actionOnDestroy: Object.Destroy);

                s_ActionPools.Add(actionID, actionPool);
            }

            return actionPool;
        }


        /// <summary>
        /// Factory method that creates Actions from their request data.
        /// </summary>
        /// <param name="data">the data to instantiate this skill from. </param>
        /// <returns>the newly created action. </returns>
        public static Action CreateActionFromData(ref ActionRequestData data)
        {
            var ret = GetActionPool(data.ActionID).Get();
            ret.Initialize(ref data);
            return ret;
        }

        public static void ReturnAction(Action action)
        {
            var pool = GetActionPool(action.ActionID);
            pool.Release(action);
        }

        public static void PurgePooledActions()
        {
            foreach (var actionPool in s_ActionPools.Values)
            {
                actionPool.Clear();
            }
        }
    }
}
