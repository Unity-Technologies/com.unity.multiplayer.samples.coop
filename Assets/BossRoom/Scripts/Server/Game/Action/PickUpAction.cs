using Unity.Netcode;
using UnityEngine;

namespace BossRoom.Server
{
    public class PickUpAction : Action
    {
        RaycastHit[] m_RaycastHits = new RaycastHit[1];

        const string k_HeavyTag = "Heavy";

        CustomParentingHandler m_CustomParentingHandler;

        public PickUpAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            m_CustomParentingHandler = parent.physicsWrapper.Transform.GetComponent<CustomParentingHandler>();
        }

        public override bool Start()
        {
            // first check if a pot has already been parented; if so, drop it
            var serverRaisePot = m_Parent.physicsWrapper.Transform.GetComponentInChildren<ServerDisplaceOnParentChange>();
            if (serverRaisePot)
            {
                if (m_CustomParentingHandler.TryRemoveParent(serverRaisePot.NetworkObject))
                {
                    serverRaisePot.NetworkObjectParentChanged(null);
                }
                Data.TargetIds = null;
                m_Parent.NetState.RecvDoActionClientRPC(Data);
                return false;
            }

            var numResults = Physics.BoxCastNonAlloc(m_Parent.physicsWrapper.Transform.position, m_Parent.physicsWrapper.DamageCollider.bounds.extents,
                m_Parent.physicsWrapper.Transform.forward, m_RaycastHits, Quaternion.identity, Description.Range, 1 << LayerMask.NameToLayer("NPCs"));

            // collider must contain "Heavy" tag
            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject) ||
                !m_RaycastHits[0].collider.gameObject.CompareTag(k_HeavyTag))
            {
                return false;
            }

            if (!m_CustomParentingHandler.TrySetParent(heavyNetworkObject))
            {
                return false;
            }

            if (heavyNetworkObject.TryGetComponent(out serverRaisePot))
            {
                serverRaisePot.NetworkObjectParentChanged(m_Parent.NetworkObject);
            }

            Data.TargetIds = new ulong[] { heavyNetworkObject.NetworkObjectId };

            // snap to face the right direction
            if( Data.Direction != Vector3.zero )
            {
                m_Parent.transform.forward = Data.Direction;
            }

            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            return ActionConclusion.Stop;
        }
    }
}
