using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace BossRoom
{
    /// <summary>
    /// This class is designed to work hand in hand with the GameEvent class, which is a ScriptableObject container
    /// for an event. GameEventListener declares a GameEvent, which is to be assigned within the editor. When the
    /// GameEvent's Raise() method is fired, this class will invoke a UnityEvent.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField]
        GameEvent m_Event;

        [SerializeField]
        UnityEvent m_Response;

        void OnEnable()
        {
            Assert.IsNotNull(m_Event, "Assign this GameEvent within the editor!");

            m_Event.RegisterListener(this);
        }

        void OnDisable()
        {
            m_Event.DeregisterListener(this);
        }

        public void EventRaised()
        {
            m_Response.Invoke();
        }
    }
}
