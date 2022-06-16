using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class ClientTossedItemVisualizer : NetworkBehaviour
    {
        [SerializeField]
        Transform m_TossedItemVisualTransform;

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
            m_TossedItemVisualTransform.gameObject.SetActive(true);
        }

        public override void OnNetworkDespawn()
        {
            m_TossedItemVisualTransform.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var tossedItemPosition = transform.position;
            m_TossedItemVisualTransform.SetPositionAndRotation(
                new Vector3(tossedItemPosition.x, k_DisplayHeight, tossedItemPosition.z),
                k_TossAttackDisplayRotation);
        }
    }
}
