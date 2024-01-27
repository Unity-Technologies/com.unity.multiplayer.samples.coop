using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public static class Event
    {
        private static Dictionary<Type, List<EventListenerBase>> _listenerListByEvent = new();

        public static void AddListener<EventType>(IEventListener<EventType> listener) where EventType : struct
        {
            Type eventType = typeof(EventType);

            if (!_listenerListByEvent.ContainsKey(eventType))
            {
                _listenerListByEvent[eventType] = new List<EventListenerBase>();
            }

            if (!ListenerExists(eventType, listener))
            {
                _listenerListByEvent[eventType].Add(listener);
            }
        }

        public static void RemoveListener<EventType>(IEventListener<EventType> listener) where EventType : struct
        {
            Type eventType = typeof(EventType);

            if (!_listenerListByEvent.ContainsKey(eventType))
            {
                return;
            }

            List<EventListenerBase> listenerList = _listenerListByEvent[eventType];

            for (int i = listenerList.Count - 1; i >= 0; i--)
            {
                if (listenerList[i] == listener)
                {
                    listenerList.Remove(listenerList[i]);

                    if (listenerList.Count == 0)
                    {
                        _listenerListByEvent.Remove(eventType);
                    }

                    return;
                }
            }
        }

        public static void Emit<EventType>(EventType newEvent) where EventType : struct
        {
            List<EventListenerBase> list;
            if (!_listenerListByEvent.TryGetValue(typeof(EventType), out list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as IEventListener<EventType>).OnEventTriggered(newEvent);
            }
        }

        private static bool ListenerExists(Type type, EventListenerBase receiver)
        {
            List<EventListenerBase> receivers;

            if (!_listenerListByEvent.TryGetValue(type, out receivers)) return false;

            bool exists = false;

            for (int i = receivers.Count - 1; i >= 0; i--)
            {
                if (receivers[i] == receiver)
                {
                    exists = true;
                    break;
                }
            }

            return exists;
        }
    }

    public interface EventListenerBase { };

    public interface IEventListener<EventType> : EventListenerBase
    {
        void OnEventTriggered(EventType eventObject);
    }

    class EventListener<EventType> : IEventListener<EventType> where EventType : struct
    {
        private Action<EventType> _func;

        EventListener(Action<EventType> func) {
            _func = func;
            Event.AddListener(this);
        }

        ~EventListener()
        {
            Event.RemoveListener(this);
        }

        public void OnEventTriggered(EventType eventObject) {
            _func(eventObject);
        }
    }
}
