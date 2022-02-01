using UnityEngine;
using UnityEngine.Events;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    /// <summary>
    /// MonoBehaviour that will automatically handle setting up to observe something. It also exposes an event so some other component can effectively observe it as well.
    /// </summary>
    public abstract class ObserverBehaviour<T> : MonoBehaviour where T : Observed<T>
    {
        public T Observed { get; set; }
        public UnityEvent<T> OnObservedUpdated;

        protected virtual void UpdateObserver(T obs)
        {
            OnObservedUpdated?.Invoke(Observed);
        }

        public void BeginObserving(T target)
        {
            if (target == null)
            {
                Debug.LogError($"Needs a Target of type {typeof(T)} to begin observing.", gameObject);
                return;
            }
            Observed = target;
            UpdateObserver(target);
            Observed.onChanged += UpdateObserver;
        }

        public void EndObserving()
        {
            if (Observed == null)
            {
                return;
            }

            Observed.onChanged -= UpdateObserver;
            Observed = null;
        }

        void Awake()
        {
            if (Observed == null)
                return;
            BeginObserving(Observed);
        }

        void OnDestroy()
        {
            if (Observed == null)
                return;
            EndObserving();
        }
    }
}
