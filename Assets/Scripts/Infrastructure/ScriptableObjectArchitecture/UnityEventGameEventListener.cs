using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// This class implements the IGameEventListener interface and exposes a GameEvent that we can populate within the
    /// inspector. When this GameEvent's Raise() method is fired externally, this class will invoke a UnityEvent.
    /// </summary>
    public class UnityEventGameEventListener : MonoBehaviour, IGameEventListenable
    {
        [SerializeField]
        GameEvent m_GameEvent;

        [SerializeField]
        UnityEvent m_Response;

        public GameEvent GameEvent
        {
            get => m_GameEvent;
            set => m_GameEvent = value;
        }

        void OnEnable()
        {
            Assert.IsNotNull(GameEvent, "Assign this GameEvent within the editor!");

            GameEvent.RegisterListener(this);
        }

        void OnDisable()
        {
            GameEvent.DeregisterListener(this);
        }

        public void EventRaised()
        {
            m_Response.Invoke();
        }
    }
}
