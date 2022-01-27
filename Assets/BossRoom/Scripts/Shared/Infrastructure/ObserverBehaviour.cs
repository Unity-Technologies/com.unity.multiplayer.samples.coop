using UnityEngine;
using UnityEngine.Events;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    /// <summary>
    /// MonoBehaviour that will automatically handle setting up to observe something. It also exposes an event so some other component can effectively observe it as well.
    /// </summary>
    public abstract class ObserverBehaviour<T> : MonoBehaviour where T : Observed<T>
    {
        public T observed { get; set; }
        public UnityEvent<T> OnObservedUpdated;

        protected virtual void UpdateObserver(T obs)
        {
            OnObservedUpdated?.Invoke(observed);
        }

        public void BeginObserving(T target)
        {
            if (target == null)
            {
                Debug.LogError($"Needs a Target of type {typeof(T)} to begin observing.", gameObject);
                return;
            }
            observed = target;
            UpdateObserver(target);
            observed.onChanged += UpdateObserver;
        }

        public void EndObserving()
        {
            if (observed == null)
            {
                return;
            }

            observed.onChanged -= UpdateObserver;
            observed = null;
        }

        void Awake()
        {
            if (observed == null)
                return;
            BeginObserving(observed);
        }

        void OnDestroy()
        {
            if (observed == null)
                return;
            EndObserving();
        }
    }
}
