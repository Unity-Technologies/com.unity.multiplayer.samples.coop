using System;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Generic movement object that updates transforms based on the state of an INetMovement source.
    /// This is part of a temporary movement system that will be replaced once Netcode for GameObjects can drive
    /// movement internally.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientGenericMovement : MonoBehaviour
    {
        private INetMovement m_MovementSource;
        private Rigidbody m_Rigidbody;
        private bool m_Initialized;

        void Awake()
        {
            GetComponent<NetcodeHooks>().OnNetworkSpawnHook += OnSpawn;
        }

        void OnDestroy()
        {
            GetComponent<NetcodeHooks>().OnNetworkSpawnHook -= OnSpawn;
        }

        void Start()
        {
            m_MovementSource = GetComponent<INetMovement>();
            m_Rigidbody = GetComponent<Rigidbody>(); //this may be null.
        }

        void OnSpawn()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                //this component is not needed on the host (or dedicated server), because ServerCharacterMovement will directly
                //update the character's position.
                this.enabled = false;
            }
            m_Initialized = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_Initialized) { return; }

            transform.position = m_MovementSource.NetworkPosition.Value;
            transform.rotation = Quaternion.Euler(0, m_MovementSource.NetworkRotationY.Value, 0);

            if (m_Rigidbody != null)
            {
                m_Rigidbody.position = transform.position;
                m_Rigidbody.rotation = transform.rotation;
            }
        }
    }
}

