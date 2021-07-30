using System;
using BossRoom.Visual;
using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Client-side component that awaits a state change on an avatar's Guid, and fetches matching Avatar from the
    /// AvatarRegistry, if possible. Once fetched, the Graphics GameObject is spawned.
    /// </summary>
    [RequireComponent(typeof(NetworkAvatarGuidState))]
    public class ClientAvatarGuidHandler : NetworkBehaviour
    {
        [SerializeField]
        ClientPlayerAvatarRuntimeCollection m_ClientPlayerAvatars;

        [SerializeField]
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;

        Avatar m_Avatar;

        public Avatar RegisteredAvatar => m_Avatar;

        public event Action<GameObject> AvatarGraphicsSpawned;

        ClientPlayerAvatar m_ClientPlayerAvatar;

        public override void OnNetworkSpawn()
        {
            if (m_ClientPlayerAvatars.TryGetPlayer(OwnerClientId, out var clientPlayerAvatar))
            {
                TryRegisterClientPlayerAvatar(clientPlayerAvatar);
            }
            else
            {
                m_ClientPlayerAvatars.ItemAdded += TryRegisterClientPlayerAvatar;
            }
        }

        void TryRegisterClientPlayerAvatar(ClientPlayerAvatar clientPlayerAvatar)
        {
            if (clientPlayerAvatar.OwnerClientId == OwnerClientId)
            {
                m_ClientPlayerAvatar = clientPlayerAvatar;

                if (m_NetworkAvatarGuidState.AvatarGuidArray.Value != null ||
                    m_NetworkAvatarGuidState.AvatarGuidArray.Value.Length == 16)
                {
                    // not a valid Guid
                    RegisterAvatar(new Guid(m_NetworkAvatarGuidState.AvatarGuidArray.Value));
                }
                else
                {
                    // TODO unsubscribe
                    m_NetworkAvatarGuidState.GuidChanged += RegisterAvatar;
                }
            }
        }

        void RegisterAvatar(Guid guid)
        {
            // based on the Guid received, Avatar is fetched from AvatarRegistry
            if (!m_AvatarRegistry.TryGetAvatar(guid, out var avatar))
            {
                Debug.LogError("Avatar not found!");
                return;
            }

            if (m_ClientPlayerAvatar.TryGetComponent(out ClientCharacter clientCharacter) &&
                clientCharacter.ChildVizObject)
            {
                // we may receive a NetworkVariable's OnValueChanged callback more than once as a client
                // this makes sure we don't spawn a duplicate graphics GameObject
                return;
            }

            m_Avatar = avatar;

            var animatorParent = GetComponentInChildren<Animator>();

            // spawn avatar graphics GameObject
            var graphicsGameObject = Instantiate(avatar.Graphics, animatorParent.transform);

            clientCharacter.SetCharacterVisualization(graphicsGameObject.GetComponent<ClientCharacterVisualization>());

            animatorParent.Rebind();
            animatorParent.Update(0f);

            AvatarGraphicsSpawned?.Invoke(graphicsGameObject);
        }

        void OnDestroy()
        {
            if (m_NetworkAvatarGuidState)
            {
                m_NetworkAvatarGuidState.GuidChanged -= RegisterAvatar;
            }
        }
    }
}
