using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerTossedItem : NetworkBehaviour
    {
        [SerializeField]
        int m_DamagePoints;

        [SerializeField]
        float m_HitRadius = 5f;

        [SerializeField]
        float m_KnockbackSpeed;

        [SerializeField]
        float m_KnockbackDuration;

        [SerializeField]
        LayerMask m_LayerMask;

        bool m_Started;

        const int k_MaxCollisions = 16;

        Collider[] m_CollisionCache = new Collider[k_MaxCollisions];

        [SerializeField]
        float detonateAfterSeconds = 5f;

        float m_DetonateAfterSeconds;
        
        [SerializeField]
        float destroyAfterSeconds = 6f;

        float m_DestroyAfterSeconds;

        bool m_Detonated;
        
        public UnityEvent detonatedCallback;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_Started = true;
            m_Detonated = false;

            m_DetonateAfterSeconds = Time.fixedTime + detonateAfterSeconds;
            m_DestroyAfterSeconds = Time.fixedTime + destroyAfterSeconds;
        }

        public override void OnNetworkDespawn()
        {
            m_Started = false;
            m_Detonated = false;
        }

        void Detonate()
        {
            var hits = Physics.OverlapSphereNonAlloc(transform.position, m_HitRadius, m_CollisionCache, m_LayerMask);

            for (int i = 0; i < hits; i++)
            {
                if (m_CollisionCache[i].gameObject.TryGetComponent(out IDamageable damageReceiver))
                {
                    damageReceiver.ReceiveHP(null, -m_DamagePoints);

                    var serverCharacter = m_CollisionCache[i].gameObject.GetComponentInParent<ServerCharacter>();
                    if (serverCharacter)
                    {
                        serverCharacter.Movement.StartKnockback(transform.position, m_KnockbackSpeed, m_KnockbackDuration);
                    }
                }
            }
            
            // send client RPC to detonate on clients
            DetonateClientRpc();

            m_Detonated = true;
        }

        [ClientRpc]
        void DetonateClientRpc()
        {
            detonatedCallback?.Invoke();
        }

        void FixedUpdate()
        {
            if (!m_Started)
            {
                return; //don't do anything before OnNetworkSpawn has run.
            }

            if (!m_Detonated && m_DetonateAfterSeconds < Time.fixedTime)
            {
                Detonate();
            }

            if (m_Detonated && m_DestroyAfterSeconds < Time.deltaTime)
            {
                // despawn after sending detonate RPC
                var networkObject = gameObject.GetComponent<NetworkObject>();
                networkObject.Despawn();
            }
        }
    }
}

