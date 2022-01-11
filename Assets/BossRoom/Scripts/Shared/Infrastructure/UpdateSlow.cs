using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    public delegate void UpdateMethod(float dt);

    /// <summary>
    /// Some objects might need to be on a slower update loop than the usual MonoBehaviour Update and without precise timing, e.g. to refresh data from services.
    /// Some might also not want to be coupled to a Unity object at all but still need an update loop.
    /// </summary>
    public class UpdateSlow : MonoBehaviour, IUpdateSlow
    {
        private class Subscriber
        {
            public UpdateMethod updateMethod;
            public readonly float period;
            public float periodCurrent;
            public Subscriber(UpdateMethod updateMethod, float period)
            {
                this.updateMethod = updateMethod;
                this.period = period;
                this.periodCurrent = 0;
            }
        }

        [SerializeField]
        [Tooltip("If a subscriber to slow update takes longer than this to execute, it can be automatically unsubscribed.")]
        private float m_durationToleranceMs = 10;
        [SerializeField]
        [Tooltip("We ordinarily automatically remove a subscriber that takes too long. Otherwise, we'll simply log.")]
        private bool m_doNotRemoveIfTooLong = false;
        private List<Subscriber> m_subscribers = new List<Subscriber>();


        [Inject]
        private void Inject()
        {

        }

        public void OnDestroy()
        {
            m_subscribers.Clear(); // We should clean up references in case they would prevent garbage collection.
        }

        /// <summary>
        /// Subscribe in order to have onUpdate called approximately every period seconds (or every frame, if period <= 0).
        /// Don't assume that onUpdate will be called in any particular order compared to other subscribers.
        /// </summary>
        public void Subscribe(UpdateMethod onUpdate, float period)
        {
            if (onUpdate == null)
                return;
            foreach (Subscriber currSub in m_subscribers)
                if (currSub.updateMethod.Equals(onUpdate))
                    return;
            m_subscribers.Add(new Subscriber(onUpdate, period));
        }
        /// <summary>Safe to call even if onUpdate was not previously Subscribed.</summary>
        public void Unsubscribe(UpdateMethod onUpdate)
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
            for (int s = m_subscribers.Count - 1; s >= 0; s--) // Iterate in reverse in case we need to remove something.
            {
                var sub = m_subscribers[s];
                sub.periodCurrent += Time.deltaTime;
                if (sub.periodCurrent > sub.period)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    UpdateMethod onUpdate = sub.updateMethod;
                    if (onUpdate == null) // In case something forgets to Unsubscribe when it dies.
                    {   Remove(s, $"Did not Unsubscribe from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                        continue;
                    }
                    if (onUpdate.Target == null) // Detect a local function that cannot be Unsubscribed since it could go out of scope.
                    {   Remove(s, $"Removed local function from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                        continue;
                    }
                    if (onUpdate.Method.ToString().Contains("<")) // Detect an anonymous function that cannot be Unsubscribed, by checking for a character that can't exist in a declared method name.
                    {   Remove(s, $"Removed anonymous from UpdateSlow: {onUpdate.Target} : {onUpdate.Method}");
                        continue;
                    }

                    stopwatch.Restart();
                    onUpdate?.Invoke(sub.periodCurrent);
                    stopwatch.Stop();
                    sub.periodCurrent = 0;

                    if (stopwatch.ElapsedMilliseconds > m_durationToleranceMs)
                    {
                        if (!m_doNotRemoveIfTooLong)
                            Remove(s, $"UpdateSlow subscriber took too long, removing: {onUpdate.Target} : {onUpdate.Method}");
                        else
                            Debug.LogWarning($"UpdateSlow subscriber took too long: {onUpdate.Target} : {onUpdate.Method}");
                    }
                }
            }

            void Remove(int index, string msg)
            {
                m_subscribers.RemoveAt(index);
                Debug.LogError(msg);
            }
        }

        public void OnReProvided(IUpdateSlow prevUpdateSlow)
        {
            if (prevUpdateSlow is UpdateSlow)
                m_subscribers.AddRange((prevUpdateSlow as UpdateSlow).m_subscribers);
        }
    }

    public interface IUpdateSlow
    {
        void OnUpdate(float dt);
        void Subscribe(UpdateMethod onUpdate, float period);
        void Unsubscribe(UpdateMethod onUpdate);
    }

    /// <summary>
    /// A default implementation.
    /// </summary>
    public class UpdateSlowNoop : IUpdateSlow
    {
        public void OnUpdate(float dt) { }
        public void Subscribe(UpdateMethod onUpdate, float period) { }
        public void Unsubscribe(UpdateMethod onUpdate) { }
        public void OnReProvided(IUpdateSlow prev) { }
    }

}
