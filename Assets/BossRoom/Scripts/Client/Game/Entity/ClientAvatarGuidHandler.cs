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
        ClientCharacter m_ClientCharacter;

        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        [SerializeField]
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;

        void Awake()
        {
            m_NetworkAvatarGuidState.GuidChanged += RegisterAvatar;
        }

        void RegisterAvatar(Guid guid)
        {
            // based on the Guid received, Avatar is fetched from AvatarRegistry
            if (!m_AvatarRegistry.TryGetAvatar(guid, out Avatar avatar))
            {
                Debug.LogError("Avatar not found!");
                return;
            }

            if (m_ClientCharacter.ChildVizObject)
            {
                // we may receive a NetworkVariable's OnValueChanged callback more than once as a client
                // this makes sure we don't spawn a duplicate graphics GameObject
                return;
            }

            m_CharacterClassContainer.SetCharacterClass(avatar.CharacterClass);

            // spawn avatar graphics GameObject
            var graphicsGameObject = Instantiate(avatar.Graphics, transform);

            m_ClientCharacter.ChildVizObject = graphicsGameObject.GetComponent<ClientCharacterVisualization>();
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
