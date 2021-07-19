using System;
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
                m_ServerPickup.Pickup();
            }
        }
    }
}
