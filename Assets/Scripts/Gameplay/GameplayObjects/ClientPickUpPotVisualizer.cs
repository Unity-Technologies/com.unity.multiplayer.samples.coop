using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Component to simply play a descending animation when this NetworkObject's parent NetworkObject changes.
    /// </summary>
    public class ClientPickUpPotVisualizer : NetworkBehaviour
    {
        [SerializeField]
        ParticleSystem m_PutDownParticleSystem;

        /*[SerializeField]
        AudioSource m_PickUpSound;
        
        [SerializeField]
        AudioSource m_PutDownSound;*/
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
            }
            else
            {
                
            }
        }
    }
}
