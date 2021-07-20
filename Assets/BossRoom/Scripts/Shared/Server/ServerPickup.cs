using System;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

namespace BossRoom
{
    public class ServerPickup : NetworkBehaviour
    {
        [SerializeField]
        ClientPickup m_ClientPickup;

        [SerializeField]
        Collider m_Collider;

        RaycastHit[] m_RaycastHits = new RaycastHit[1];

        NetworkObject m_CurrentHeavyObject;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
        }

        [ServerRpc]
        public void PickupServerRpc()
        {
            if (m_CurrentHeavyObject) // ie. drop current item if heavy input has been received
            {
                m_CurrentHeavyObject.transform.SetParent(null);
                m_CurrentHeavyObject = null;
                m_ClientPickup.SetAnimationTriggerClientRpc(m_CurrentHeavyObject != null);
                return;
            }

            int numResults = Physics.BoxCastNonAlloc(transform.position, m_Collider.bounds.extents,
                transform.forward, m_RaycastHits, Quaternion.identity, 5f, 1 << LayerMask.NameToLayer("Heavy"));

            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject))
            {
                return;
            }

            if (heavyNetworkObject.TrySetParent(transform))
            {
                m_CurrentHeavyObject = heavyNetworkObject;
                m_ClientPickup.SetAnimationTriggerClientRpc(m_CurrentHeavyObject != null);
            }
        }
    }
}
