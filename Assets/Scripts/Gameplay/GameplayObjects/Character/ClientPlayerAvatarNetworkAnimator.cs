using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Avatar = Unity.BossRoom.Gameplay.Configuration.Avatar;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Component that spawns a PlayerAvatar's Avatar. It does this in two places:
    /// 1) either inside OnNetworkSpawn() or
    /// 2) inside NetworkAnimator's OnSynchronize method.
    /// The latter is necessary for clients receiving initial synchronizing data, where the Animator needs to be present
    /// and bound (Animator.Bind()) *before* the incoming animation data is applied.
    /// </summary>
    public class ClientPlayerAvatarNetworkAnimator : NetworkAnimator
    {
        [HideInInspector]
        public NetworkVariable<NetworkGuid> AvatarGuid = new NetworkVariable<NetworkGuid>();

        bool m_AvatarInstantiated;

        [SerializeField] AvatarRegistry m_AvatarRegistry;

        Avatar m_Avatar;

        public Avatar RegisteredAvatar
        {
            get
            {
                if (m_Avatar == null)
                {
                    RegisterAvatar(AvatarGuid.Value.ToGuid());
                }

                return m_Avatar;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            RegisterAvatar(AvatarGuid.Value.ToGuid());
        }

        void RegisterAvatar(Guid guid)
        {
            if (guid.Equals(Guid.Empty))
            {
                // not a valid Guid
                return;
            }

            // based on the Guid received, Avatar is fetched from AvatarRegistry
            if (!m_AvatarRegistry.TryGetAvatar(guid, out var avatar))
            {
                Debug.LogError("Avatar not found!");
                return;
            }

            if (m_Avatar != null)
            {
                // already set, this is an idempotent call, we don't want to Instantiate twice
                return;
            }

            m_Avatar = avatar;
        }

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (!IsClient || m_AvatarInstantiated)
            {
                return;
            }

            InstantiateAvatar();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_AvatarInstantiated = false;
            var avatarGraphics = Animator.transform.GetChild(0);
            if (avatarGraphics != null)
            {
                Destroy(avatarGraphics.gameObject);
            }
        }

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            if (NetworkManager.Singleton.IsClient && !m_AvatarInstantiated)
            {
                InstantiateAvatar();
            }

            base.OnSynchronize(ref serializer);
        }

        void InstantiateAvatar()
        {
            if (Animator.transform.childCount > 0)
            {
                // we may receive a NetworkVariable's OnValueChanged callback more than once as a client
                // this makes sure we don't spawn a duplicate graphics GameObject
                return;
            }

            // spawn avatar graphics GameObject
            Instantiate(RegisteredAvatar.Graphics, Animator.transform);

            Animator.Rebind();

            m_AvatarInstantiated = true;
        }
    }
}
