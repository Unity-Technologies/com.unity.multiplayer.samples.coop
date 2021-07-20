using System;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

namespace BossRoom
{
    [RequireComponent(typeof(ServerPickup))]
    public class ClientPickup : NetworkBehaviour
    {
        [SerializeField]
        ServerPickup m_ServerPickup;

        [SerializeField]
        Animator m_Animator;

        const string k_PickupBool = "IsCarrying";

        int m_PickupAnimationID;

        void Awake()
        {
            m_PickupAnimationID = Animator.StringToHash(k_PickupBool);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                m_ServerPickup.PickupServerRpc();
            }
        }

        [ClientRpc]
        public void SetAnimationTriggerClientRpc(bool isCarrying)
        {
            m_Animator.SetBool(m_PickupAnimationID, isCarrying);
        }
    }
}
