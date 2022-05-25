using System;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientPlayerAvatar : MonoBehaviour
    {
        [SerializeField]
        ClientPlayerAvatarRuntimeCollection m_PlayerAvatars;

        public NetcodeHooks NetcodeHooks;

        public static event Action<ClientPlayerAvatar> LocalClientSpawned;

        public static event Action LocalClientDespawned;

        void Awake()
        {
            NetcodeHooks = GetComponent<NetcodeHooks>();
            NetcodeHooks.OnNetworkSpawnHook += OnSpawn;
            NetcodeHooks.OnNetworkDespawnHook += OnDespawn;
        }

        void OnSpawn()
        {
            var networkManager = NetworkManager.Singleton;
            name = "PlayerAvatar" + NetcodeHooks.OwnerClientId;

            if (networkManager.IsClient && NetcodeHooks.IsOwner)
            {
                LocalClientSpawned?.Invoke(this);
            }

            if (m_PlayerAvatars)
            {
                m_PlayerAvatars.Add(this);
            }
        }

        void OnDespawn()
        {
            if (NetworkManager.Singleton.IsClient && NetcodeHooks.IsOwner)
            {
                LocalClientDespawned?.Invoke();
            }

            RemoveNetworkCharacter();
        }

        public void OnDestroy()
        {
            RemoveNetworkCharacter();
            NetcodeHooks.OnNetworkSpawnHook -= OnSpawn;
            NetcodeHooks.OnNetworkDespawnHook -= OnDespawn;
        }

        void RemoveNetworkCharacter()
        {
            if (m_PlayerAvatars)
            {
                m_PlayerAvatars.Remove(this);
            }
        }
    }
}
