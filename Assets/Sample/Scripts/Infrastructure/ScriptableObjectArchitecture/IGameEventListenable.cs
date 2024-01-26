namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// This class is designed to work hand in hand with the GameEvent class, which is a ScriptableObject container
    /// for an event. IGameEventListener declares a GameEvent, which is to be defined by the implementing class. The
    /// behaviour for EventRaised(), as well as when to register/deregister this listener will also be handled by
    /// the implementing class.
    /// </summary>
    public interface IGameEventListenable
    {
        public GameEvent GameEvent { get; set; }

        public void EventRaised();
    }
}
