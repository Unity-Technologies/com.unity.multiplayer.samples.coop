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
    [RequireComponent(typeof(NetworkGuidState))]
    public class ClientAvatarGuidHandler : NetworkBehaviour
    {
        [SerializeField]
        ClientCharacter m_ClientCharacter;

        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        [SerializeField]
        NetworkGuidState m_NetworkGuidState;

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;

        void Awake()
        {
            m_NetworkGuidState.GuidChanged += RegisterAvatar;
        }

        void RegisterAvatar(Guid guid)
        {
            // based on the Guid received, Avatar is fetched from AvatarRegistry
            if (!m_AvatarRegistry.TryGetAvatar(guid, out Avatar avatar))
            {
                Debug.LogError("Avatar not found!");
            }

            m_CharacterClassContainer.SetCharacterClass(avatar.CharacterClass);

            // spawn avatar graphics GameObject
            var graphicsGameObject = Instantiate(avatar.Graphics, transform);
            graphicsGameObject.name = "AvatarGraphics" + OwnerClientId;

            m_ClientCharacter.ChildVizObject = graphicsGameObject.GetComponent<ClientCharacterVisualization>();
        }
    }
}
