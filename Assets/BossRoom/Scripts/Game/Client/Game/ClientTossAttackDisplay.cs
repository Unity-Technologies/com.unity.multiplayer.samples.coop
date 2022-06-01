using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class ClientTossAttackDisplay : NetworkBehaviour
    {
        [SerializeField]
        Transform m_BombDisplayTransform;

        const float k_DisplayHeight = 0.1f;

        readonly Quaternion k_BombDisplayRotation = Quaternion.Euler(90f, 0f, 0f);

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
            m_BombDisplayTransform.gameObject.SetActive(true);
        }

        public override void OnNetworkDespawn()
        {
            m_BombDisplayTransform.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var tossedItemPosition = transform.position;
            m_BombDisplayTransform.SetPositionAndRotation(
                new Vector3(tossedItemPosition.x, k_DisplayHeight, tossedItemPosition.z),
                k_BombDisplayRotation);
        }
    }
}
