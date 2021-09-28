using UnityEngine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class CustomParentingHandler : NetworkBehaviour
    {
        public bool preserveWorldSpace = true;

        NetworkList<ulong> m_MyNetworkObjectChildren = new NetworkList<ulong>();

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                m_MyNetworkObjectChildren.OnListChanged += MyNetworkObjectChildren_OnListChanged;
            }
            base.OnNetworkSpawn();
        }

        private void MyNetworkObjectChildren_OnListChanged(NetworkListEvent<ulong> changeEvent)
        {
            switch( changeEvent.Type )
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

        private void AddChildNetworkObject(ulong networkObjectId)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
            {
                if (!OnTrySetParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId]))
                {
                    //Error message
                }
            }
        }

        private void RemoveChildNetworkObject(ulong networkObjectId)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
            {
                if(!OnTryRemoveParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId]))
                {
                    // Error message
                }
            }
        }

        private void RemoveAllChildren()
        {
            var children = GetComponentsInChildren<NetworkObject>();
            foreach (var child in children)
            {
                if (child.transform.parent == transform)
                {
                    if(!TryRemoveParent(child))
                    {
                        //Error message
                    }
                }
            }
        }

        private bool ValidateParenting(NetworkObject child, bool shouldParent)
        {
            if (shouldParent)
            {
                return IsServer ? !m_MyNetworkObjectChildren.Contains(child.NetworkObjectId) : m_MyNetworkObjectChildren.Contains(child.NetworkObjectId);
            }
            else
            {
                return IsServer ? m_MyNetworkObjectChildren.Contains(child.NetworkObjectId) : !m_MyNetworkObjectChildren.Contains(child.NetworkObjectId);
            }
        }

        private bool OnTrySetParent(NetworkObject childToParent)
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
                    m_MyNetworkObjectChildren.Add(childToParent.NetworkObjectId);
                }

                return result;
            }
            return false;
        }

        private bool OnTryRemoveParent(NetworkObject childToRemove)
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
                    m_MyNetworkObjectChildren.Remove(childToRemove.NetworkObjectId);
                }

                return result;
            }
            return false;
        }
    }
}
