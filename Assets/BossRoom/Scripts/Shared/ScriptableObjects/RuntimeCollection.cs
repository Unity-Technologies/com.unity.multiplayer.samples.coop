using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// ScriptableObject class that contains a list of a given type. The instance of this ScriptableObject can be
    /// referenced by components, without a hard reference between systems.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RuntimeCollection<T> : ScriptableObject
    {
        public List<T> Items = new List<T>();

        public event Action ListChanged;

        public void Add(T item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);

                ListChanged?.Invoke();
            }
        }

        public void Remove(T item)
        {
            Items.Remove(item);

            ListChanged?.Invoke();
        }
    }
}
