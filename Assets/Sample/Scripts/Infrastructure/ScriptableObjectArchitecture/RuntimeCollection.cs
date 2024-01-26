using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// ScriptableObject class that contains a list of a given type. The instance of this ScriptableObject can be
    /// referenced by components, without a hard reference between systems.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RuntimeCollection<T> : ScriptableObject
    {
        public List<T> Items = new List<T>();

        public event Action<T> ItemAdded;

        public event Action<T> ItemRemoved;

        public void Add(T item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                ItemAdded?.Invoke(item);
            }
        }

        public void Remove(T item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                ItemRemoved?.Invoke(item);
            }
        }
    }
}
