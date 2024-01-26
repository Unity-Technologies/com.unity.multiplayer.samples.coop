using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Component to play VFX and SFX when this NetworkObject's parent NetworkObject changes to make the action look more polished.
    /// </summary>
    public class ClientPickUpPotEffects : NetworkBehaviour
    {
        [SerializeField]
        ParticleSystem m_PutDownParticleSystem;

        [SerializeField]
        AudioSource m_PickUpSound;

        [SerializeField]
        AudioSource m_PutDownSound;

        void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsClient;
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (!IsClient)
            {
                return;
            }

            if (parentNetworkObject == null)
            {
                m_PutDownParticleSystem.Play();
                m_PutDownSound.Play();
            }
            else
            {
                m_PickUpSound.Play();
            }
        }
    }
}
