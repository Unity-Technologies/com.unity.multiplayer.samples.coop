using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class ClientTossAttackDisplay : NetworkBehaviour
    {
        [SerializeField]
        Transform m_TossAttackDisplayTransform;

        const float k_DisplayHeight = 0.1f;

        readonly Quaternion k_TossAttackDisplayRotation = Quaternion.Euler(90f, 0f, 0f);

        void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                return;
            }

            enabled = true;
            m_TossAttackDisplayTransform.gameObject.SetActive(true);
        }

        public override void OnNetworkDespawn()
        {
            m_TossAttackDisplayTransform.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var tossedItemPosition = transform.position;
            m_TossAttackDisplayTransform.SetPositionAndRotation(
                new Vector3(tossedItemPosition.x, k_DisplayHeight, tossedItemPosition.z),
                k_TossAttackDisplayRotation);
        }
    }
}
