using System;
using MLAPI.Messaging;
using UnityEngine;

namespace BossRoom
{
    [RequireComponent(typeof(ServerPickup))]
    public class ClientPickup : MonoBehaviour
    {
        [SerializeField]
        ServerPickup m_ServerPickup;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                PickupServerRpc();
            }
        }

        [ServerRpc]
        void PickupServerRpc()
        {
            m_ServerPickup.Pickup();
        }
    }
}
