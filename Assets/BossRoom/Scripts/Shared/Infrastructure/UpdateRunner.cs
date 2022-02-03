using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Infrastructure
{

    /// <summary>
    /// Some objects might need to be on a slower update loop than the usual MonoBehaviour Update and without precise timing, e.g. to refresh data from services.
    /// Some might also not want to be coupled to a Unity object at all but still need an update loop.
    /// </summary>
    public class UpdateRunner : MonoBehaviour
    {
        private class Subscriber
        {
            public Action<float> updateMethod;
            public readonly float period;
            public float periodCurrent;
            public Subscriber(Action<float> updateMethod, float period)
            {
                this.updateMethod = updateMethod;
                this.period = period;
                this.periodCurrent = 0;
            }
        }

        private List<Subscriber> m_subscribers = new List<Subscriber>();

        public void OnDestroy()
        {
            m_subscribers.Clear(); // We should clean up references in case they would prevent garbage collection.
        }

        /// <summary>
        /// Subscribe in order to have onUpdate called approximately every period seconds (or every frame, if period <= 0).
        /// Don't assume that onUpdate will be called in any particular order compared to other subscribers.
        /// </summary>
        public void Subscribe(Action<float> onUpdate, float period)
        {
            if (onUpdate == null)
            {
                return;
            }

            if (onUpdate.Target == null) // Detect a local function that cannot be Unsubscribed since it could go out of scope.
            {
                Debug.LogError("Can't subscribe to a local function that can go out of scope and can't be unsubscribed from");
                return;
            }

            if (onUpdate.Method.ToString().Contains("<")) // Detect
            {
                Debug.LogError("Can't subscribe with an anonymous function that cannot be Unsubscribed, by checking for a character that can't exist in a declared method name.");
                return;
            }

            foreach (Subscriber currSub in m_subscribers)
                if (currSub.updateMethod.Equals(onUpdate))
                    return;
            m_subscribers.Add(new Subscriber(onUpdate, period));
        }
        /// <summary>Safe to call even if onUpdate was not previously Subscribed.</summary>
        public void Unsubscribe(Action<float> onUpdate)
        {
            for (int sub = m_subscribers.Count - 1; sub >= 0; sub--)
                if (m_subscribers[sub].updateMethod.Equals(onUpdate))
                    m_subscribers.RemoveAt(sub);

        }

        private void Update()
        {
            OnUpdate(Time.deltaTime);
        }

        /// <summary>
        /// Each frame, advance all subscribers. Any that have hit their period should then act, though if they take too long they could be removed.
        /// </summary>
        public void OnUpdate(float dt)
        {
            for (int subscriberIndex = m_subscribers.Count - 1; subscriberIndex >= 0; subscriberIndex--) // Iterate in reverse in case we need to remove something.
            {
                var subscriber = m_subscribers[subscriberIndex];
                subscriber.periodCurrent += dt;

                if (subscriber.periodCurrent > subscriber.period)
                {
                    Action<float> onUpdate = subscriber.updateMethod;

                    if (onUpdate == null)
                    {
                        Remove(subscriberIndex, $"Did not Unsubscribe from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                        continue;
                    }

                    onUpdate.Invoke(subscriber.periodCurrent);
                    subscriber.periodCurrent = 0;
                }
            }

            void Remove(int index, string msg)
            {
                m_subscribers.RemoveAt(index);
                Debug.LogError(msg);
            }
        }
    }
}
