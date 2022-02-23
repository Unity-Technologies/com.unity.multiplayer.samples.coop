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
            public Action<float> UpdateMethod;
            public readonly float Period;
            public float PeriodCurrent;

            public Subscriber(Action<float> updateMethod, float period)
            {
                this.UpdateMethod = updateMethod;
                this.Period = period;
                PeriodCurrent = 0;
            }
        }

        private List<Subscriber> m_Subscribers = new List<Subscriber>();

        public void OnDestroy()
        {
            m_Subscribers.Clear(); // We should clean up references in case they would prevent garbage collection.
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

            foreach (var currSub in m_Subscribers)
            {
                if (currSub.UpdateMethod.Equals(onUpdate))
                {
                    return;
                }
            }

            m_Subscribers.Add(new Subscriber(onUpdate, period));
        }

        /// <summary>
        /// Safe to call even if onUpdate was not previously Subscribed.
        /// </summary>
        public void Unsubscribe(Action<float> onUpdate)
        {
            for (var sub = m_Subscribers.Count - 1; sub >= 0; sub--)
            {
                if (m_Subscribers[sub].UpdateMethod.Equals(onUpdate))
                {
                    m_Subscribers.RemoveAt(sub);
                }
            }
        }

        /// <summary>
        /// Each frame, advance all subscribers. Any that have hit their period should then act, though if they take too long they could be removed.
        /// </summary>
        private void Update()
        {
            float dt = Time.deltaTime;

            for (var subscriberIndex = m_Subscribers.Count - 1; subscriberIndex >= 0; subscriberIndex--) // Iterate in reverse in case we need to remove something.
            {
                var subscriber = m_Subscribers[subscriberIndex];
                subscriber.PeriodCurrent += dt;

                if (subscriber.PeriodCurrent > subscriber.Period)
                {
                    var onUpdate = subscriber.UpdateMethod;

                    if (onUpdate == null)
                    {
                        m_Subscribers.RemoveAt(subscriberIndex);
                        Debug.LogError($"Did not Unsubscribe from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                        continue;
                    }

                    onUpdate.Invoke(subscriber.PeriodCurrent);
                    subscriber.PeriodCurrent = 0;
                }
            }
        }
    }
}
