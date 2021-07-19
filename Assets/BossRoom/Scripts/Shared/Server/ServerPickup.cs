using System;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

namespace BossRoom
{
    public class ServerPickup : MonoBehaviour
    {
        [SerializeField]
        Collider m_Collider;

        [SerializeField]
        Animator m_Animator;

        RaycastHit[] m_RaycastHits = new RaycastHit[1];

        const string k_PickupBool = "IsCarrying";
        int m_PickupAnimationID;

        NetworkObject m_CurrentHeavyObject;

        void Awake()
        {
            m_PickupAnimationID = Animator.StringToHash(k_PickupBool);
        }

        [ServerRpc]
        public void Pickup()
        {
            if (m_CurrentHeavyObject) // ie. drop current item if heavy input has been received
            {
                m_CurrentHeavyObject.transform.SetParent(null);
                m_CurrentHeavyObject = null;
                SetAnimationTriggerClientRpc();
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
                SetAnimationTriggerClientRpc();
            }
        }

        [ClientRpc]
        void SetAnimationTriggerClientRpc()
        {
            m_Animator.SetBool(m_PickupAnimationID, m_CurrentHeavyObject != null);
        }
    }
}
