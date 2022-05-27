using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerItemThrow : NetworkBehaviour
    {
        [SerializeField]
        int m_DamagePoints;

        bool m_Started;

        const int k_MaxCollisions = 8;
        Collider[] m_CollisionCache = new Collider[k_MaxCollisions];

        const float k_DetonateAfterSeconds = 5f;
        float m_DetonateAtSec;

        const float k_DestroyAfterSeconds = 8f;

        // Time when we should destroy this arrow, in Time.time seconds.
        float m_DestroyAtSec;

        int m_PCLayer;

        bool m_Detonated;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_Started = true;
            m_Detonated = false;

            m_DetonateAtSec = Time.fixedTime + k_DetonateAfterSeconds;
            m_DestroyAtSec = Time.fixedTime + k_DestroyAfterSeconds;

            m_PCLayer = 1 << LayerMask.NameToLayer("PCs");
        }

        public override void OnNetworkDespawn()
        {
            m_Started = false;
            m_Detonated = false;
        }

        void Detonate()
        {
            var hits = Physics.OverlapSphereNonAlloc(transform.position, 10f, m_CollisionCache, m_PCLayer);

            for (int i = 0; i < hits; i++)
            {
                if (m_CollisionCache[i].gameObject.TryGetComponent(out IDamageable damageReceiver))
                {
                    damageReceiver.ReceiveHP(null, -m_DamagePoints);
                }
            }

            m_Detonated = true;
        }

        void FixedUpdate()
        {
            if (!m_Started)
            {
                return; //don't do anything before OnNetworkSpawn has run.
            }

            if (!m_Detonated && m_DetonateAtSec < Time.fixedTime)
            {
                Detonate();
            }

            if (m_DestroyAtSec < Time.fixedTime)
            {
                // Time to return to the pool from whence it came.
                var networkObject = gameObject.GetComponent<NetworkObject>();
                networkObject.Despawn();
            }
        }
    }
}

