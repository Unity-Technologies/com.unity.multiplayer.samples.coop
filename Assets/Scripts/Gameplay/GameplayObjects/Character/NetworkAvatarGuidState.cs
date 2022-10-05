using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Avatar = Unity.BossRoom.Gameplay.Configuration.Avatar;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// NetworkBehaviour component to send/receive GUIDs from server to clients.
    /// </summary>
    public class NetworkAvatarGuidState : NetworkBehaviour
    {
        [FormerlySerializedAs("AvatarGuidArray")]
        [HideInInspector]
        public NetworkVariable<NetworkGuid> AvatarGuid = new NetworkVariable<NetworkGuid>();

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;

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

        public void SetRandomAvatar()
        {
            AvatarGuid.Value = m_AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid();
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

            if (TryGetComponent<ServerCharacter>(out var serverCharacter))
            {
                serverCharacter.CharacterClass = avatar.CharacterClass;
            }
        }
    }
}
