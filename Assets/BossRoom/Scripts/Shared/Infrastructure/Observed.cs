using System;

namespace BossRoom.Scripts.Shared.Infrastructure
{
    /// <summary>
    /// Something that exposes some data that, when changed, an observer would want to be notified about automatically.
    /// Used for UI elements and for keeping our local Lobby state synchronized with the remote Lobby service data.
    /// (See http://gameprogrammingpatterns.com/observer.html to learn more.)
    ///
    /// In your Observed child implementations, be sure to call OnChanged when setting the value of any property.
    /// </summary>
    /// <typeparam name="T">The type of object to be observed.</typeparam>
    public abstract class Observed<T>
    {
        /// <summary>
        /// If you want to copy all of the values, and only trigger OnChanged once.
        /// </summary>
        /// <param name="oldObserved"></param>
        public abstract void CopyObserved(T oldObserved);

        public event Action<T> onChanged = delegate { };
        public event Action<T> onDestroyed = delegate { };

        /// <summary>
        /// Should be implemented into every public property of the observed
        /// </summary>
        /// <param name="observed">Instance of the observed that changed.</param>
        protected void OnChanged(T observed)
        {
            onChanged.Invoke(observed);
        }

        protected void OnDestroyed(T observed)
        {
            onDestroyed.Invoke(observed);
        }
    }
}
