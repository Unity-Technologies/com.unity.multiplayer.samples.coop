using MLAPI;
using UnityEngine;

namespace BossRoom.Server
{
    public class PickupAction : Action
    {
        private bool m_ExecutionFired;

        RaycastHit[] m_RaycastHits = new RaycastHit[1];

        public PickupAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            ulong target = (Data.TargetIds != null && Data.TargetIds.Length > 0) ? Data.TargetIds[0] : m_Parent.NetState.TargetId.Value;

            int numResults = Physics.BoxCastNonAlloc(m_Parent.transform.position, m_Parent.GetComponent<Collider>().bounds.extents,
                m_Parent.transform.forward, m_RaycastHits, Quaternion.identity, Description.Range, LayerMask.NameToLayer("Heavy"));

            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject))
            {
                return false;
            }

            heavyNetworkObject.TrySetParent(m_Parent.transform);

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
            /*if (!m_ExecutionFired && (Time.time - TimeStarted) >= Description.ExecTimeSeconds)
            {
                m_ExecutionFired = true;
                var foe = DetectFoe(m_ProvisionalTarget);
                if (foe != null)
                {
                    foe.ReceiveHP(this.m_Parent, -Description.Amount);
                }
            }*/

            return true;
        }

    }
}
