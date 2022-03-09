using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.NetworkProperties
{
    public class NetworkProperties : NetworkBehaviour
    {
        [SerializeField]
        private List<NetworkPropertySet> m_NetworkPropertySets;

        public static IReadOnlyList<T> FilterComponents<T>(IEnumerable<T> components, Filter filter) where T: Component
        {
            return new List<T>();
        }

        public static IReadOnlyList<GameObject> Filter(IEnumerable<GameObject> components, Filter filter)
        {
            return new List<GameObject>();
        }
    }

    public struct Filter
    {
        BitField64 m_Filter;

        public Filter(IEnumerable<string> names)
        {
            m_Filter = new BitField64();
        }

        public Filter(string name)
        {
            m_Filter = new BitField64();
        }
    }

    public static unsafe class NetworkPropertiesExt
    {
        public static T GetNetworkProperty<T>(this GameObject gameObject, string name)
        {
            return default;
        }

        public static ref T GetNetworkPropertyRef<T>(this GameObject gameObject, string name) where T: unmanaged
        {
            void* foo = UnsafeUtility.Malloc(sizeof(T), 8, Allocator.Temp);
            return ref UnsafeUtility.AsRef<T>(foo);
        }

        public static void SetNetworkProperty<T>(this GameObject gameObject, string name, T value)
        {

        }

        public static T GetNetworkProperty<T>(this Component component, string name)
        {
            return default;
        }

        public static ref T GetNetworkPropertyRef<T>(this Component component, string name) where T: unmanaged
        {
            void* foo = UnsafeUtility.Malloc(sizeof(T), 8, Allocator.Temp);
            return ref UnsafeUtility.AsRef<T>(foo);
        }

        public static void SetNetworkProperty<T>(this Component component, string name, T value)
        {

        }
    }
}


