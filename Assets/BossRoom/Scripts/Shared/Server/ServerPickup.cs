using MLAPI;
using UnityEngine;

namespace BossRoom
{
    public class ServerPickup : MonoBehaviour
    {
        [SerializeField]
        Collider m_Collider;

        RaycastHit[] m_RaycastHits = new RaycastHit[1];

        public void Pickup()
        {
            int numResults = Physics.BoxCastNonAlloc(transform.position, m_Collider.bounds.extents,
                transform.forward, m_RaycastHits, Quaternion.identity, 5f, 1 << LayerMask.NameToLayer("Heavy"));

            if (numResults == 0 || !m_RaycastHits[0].collider.TryGetComponent(out NetworkObject heavyNetworkObject))
            {
                return;
            }

            heavyNetworkObject.TrySetParent(transform);
        }
    }
}
