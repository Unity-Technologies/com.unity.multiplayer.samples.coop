using UnityEngine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class CustomParentingHandler : NetworkBehaviour
    {
        public bool preserveWorldSpace = true;

        NetworkList<ulong> m_NetworkObjectChildren = new NetworkList<ulong>();

        public bool hasChildren => m_NetworkObjectChildren.Count > 0;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                m_NetworkObjectChildren.OnListChanged += NetworkObjectChildrenOnListChanged;
            }
            base.OnNetworkSpawn();
        }

        void NetworkObjectChildrenOnListChanged(NetworkListEvent<ulong> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<ulong>.EventType.Add:
                    {
                        AddChildNetworkObject(changeEvent.Value);
                        break;
                    }
                case NetworkListEvent<ulong>.EventType.RemoveAt:
                case NetworkListEvent<ulong>.EventType.Remove:
                    {
                        RemoveChildNetworkObject(changeEvent.Value);
                        break;
                    }
                case NetworkListEvent<ulong>.EventType.Clear:
                    {
                        RemoveAllChildren();
                        break;
                    }
            }
        }

        void AddChildNetworkObject(ulong networkObjectId)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
            {
                if (!OnTrySetParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId]))
                {
                    //Error message
                }
            }
        }

        void RemoveChildNetworkObject(ulong networkObjectId)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
            {
                if (!OnTryRemoveParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId]))
                {
                    // Error message
                }
            }
        }

        void RemoveAllChildren()
        {
            var children = GetComponentsInChildren<NetworkObject>();
            foreach (var child in children)
            {
                if (child.transform.parent == transform)
                {
                    if (!TryRemoveParent(child))
                    {
                        //Error message
                    }
                }
            }
        }

        bool ValidateParenting(NetworkObject child, bool shouldParent)
        {
            if (shouldParent)
            {
                return IsServer ? !m_NetworkObjectChildren.Contains(child.NetworkObjectId) : m_NetworkObjectChildren.Contains(child.NetworkObjectId);
            }
            else
            {
                return IsServer ? m_NetworkObjectChildren.Contains(child.NetworkObjectId) : !m_NetworkObjectChildren.Contains(child.NetworkObjectId);
            }
        }

        bool OnTrySetParent(NetworkObject childToParent)
        {
            if (!childToParent.AutoObjectParentSync && ValidateParenting(childToParent,true))
            {
                childToParent.transform.SetParent(transform, preserveWorldSpace);
                return true;
            }
            return false;
        }

        public bool TrySetParent(NetworkObject childToParent)
        {
            if (IsServer)
            {
                var result = OnTrySetParent(childToParent);
                if (result)
                {
                    m_NetworkObjectChildren.Add(childToParent.NetworkObjectId);
                }

                return result;
            }
            return false;
        }

        bool OnTryRemoveParent(NetworkObject childToRemove)
        {
            if (!childToRemove.AutoObjectParentSync && ValidateParenting(childToRemove, false))
            {
                childToRemove.transform.SetParent(null, preserveWorldSpace);
                return true;
            }
            return false;
        }

        public bool TryRemoveParent(NetworkObject childToRemove)
        {
            if (IsServer)
            {
                var result = OnTryRemoveParent(childToRemove);
                if (result)
                {
                    m_NetworkObjectChildren.Remove(childToRemove.NetworkObjectId);
                }

                return result;
            }
            return false;
        }
    }
}
