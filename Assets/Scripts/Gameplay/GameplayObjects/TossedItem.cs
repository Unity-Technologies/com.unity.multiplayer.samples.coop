using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class TossedItem : NetworkBehaviour
    {
        [Header("Server")]

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
        float m_DetonateAfterSeconds = 5f;

        float m_DetonateTimer;

        [SerializeField]
        float m_DestroyAfterSeconds = 6f;

        float m_DestroyTimer;

        bool m_Detonated;

        public UnityEvent detonatedCallback;

        [Header("Client")]

        [SerializeField]
        Transform m_TossedItemVisualTransform;

        const float k_DisplayHeight = 0.1f;

        readonly Quaternion k_TossAttackRadiusDisplayRotation = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField]
        GameObject m_TossedObjectGraphics;

        [SerializeField]
        AudioSource m_FallingSound;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_Started = true;
                m_Detonated = false;

                m_DetonateTimer = Time.fixedTime + m_DetonateAfterSeconds;
                m_DestroyTimer = Time.fixedTime + m_DestroyAfterSeconds;
            }

            if (IsClient)
            {
                m_TossedItemVisualTransform.gameObject.SetActive(true);
                m_TossedObjectGraphics.SetActive(true);
                m_FallingSound.Play();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                m_Started = false;
                m_Detonated = false;
            }

            if (IsClient)
            {
                m_TossedItemVisualTransform.gameObject.SetActive(false);
            }

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
            ClientDetonateRpc();

            m_Detonated = true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientDetonateRpc()
        {
            detonatedCallback?.Invoke();
        }

        void FixedUpdate()
        {
            if (IsServer)
            {
                if (!m_Started)
                {
                    return; //don't do anything before OnNetworkSpawn has run.
                }

                if (!m_Detonated && m_DetonateTimer < Time.fixedTime)
                {
                    Detonate();
                }

                if (m_Detonated && m_DestroyTimer < Time.fixedTime)
                {
                    // despawn after sending detonate RPC
                    var networkObject = gameObject.GetComponent<NetworkObject>();
                    networkObject.Despawn();
                }
            }
        }

        void LateUpdate()
        {
            if (IsClient)
            {
                var tossedItemPosition = transform.position;
                m_TossedItemVisualTransform.SetPositionAndRotation(
                    new Vector3(tossedItemPosition.x, k_DisplayHeight, tossedItemPosition.z),
                    k_TossAttackRadiusDisplayRotation);
            }
        }
    }
}
