using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerRemoveChildrenOnFaint : NetworkBehaviour
    {
        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        CustomParentingHandler m_CustomParentingHandler;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_NetworkLifeState.LifeState.OnValueChanged += RemoveParentedChildren;
        }

        public override void OnNetworkDespawn()
        {
            if (m_NetworkLifeState)
            {
                m_NetworkLifeState.LifeState.OnValueChanged -= RemoveParentedChildren;
            }
        }

        void RemoveParentedChildren(LifeState previousValue, LifeState newValue)
        {
            if (newValue == LifeState.Fainted)
            {
                var children = GetComponentsInChildren<ServerDisplacerOnParentChange>();
                foreach (var child in children)
                {
                    if (m_CustomParentingHandler.TryRemoveParent(child.NetworkObject))
                    {
                        child.NetworkObjectParentChanged(null);
                    }
                }
            }
        }
    }
}
