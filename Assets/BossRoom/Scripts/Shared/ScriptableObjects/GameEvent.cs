using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Class for encapsulating game-related events within ScriptableObject instances. This class defines a List of
    /// GameEventListeners, which will be notified whenever this GameEvent's Raise() method is fired.
    /// </summary>
    [CreateAssetMenu]
    public class GameEvent : ScriptableObject
    {
        List<GameEventListener> m_Listeners = new List<GameEventListener>();

        public void Raise()
        {
            for (int i = m_Listeners.Count - 1; i >= 0; i--)
            {
                if (m_Listeners[i] == null)
                {
                    m_Listeners.RemoveAt(i);
                    continue;
                }

                m_Listeners[i].EventRaised();
            }
        }

        public void RegisterListener(GameEventListener listener)
        {
            m_Listeners.Add(listener);
        }

        public void DeregisterListener(GameEventListener listener)
        {
            m_Listeners.Remove(listener);
        }
    }
}
