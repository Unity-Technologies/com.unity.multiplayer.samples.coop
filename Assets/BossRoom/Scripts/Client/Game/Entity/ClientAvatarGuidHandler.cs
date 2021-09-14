using System;
using BossRoom.Visual;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Client-side component that awaits a state change on an avatar's Guid, and fetches matching Avatar from the
    /// AvatarRegistry, if possible. Once fetched, the Graphics GameObject is spawned.
    /// </summary>
    [RequireComponent(typeof(NetworkAvatarGuidState))]
    public class ClientAvatarGuidHandler : MonoBehaviour
    {
        [SerializeField]
        ClientCharacter m_ClientCharacter;

        [SerializeField]
        Animator m_GraphicsAnimator;

        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        [SerializeField]
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        [SerializeField]
        AvatarRegistry m_AvatarRegistry;

        Avatar m_Avatar;

        public Animator graphicsAnimator => m_GraphicsAnimator;

        public Avatar RegisteredAvatar => m_Avatar;

        public event Action<GameObject> AvatarGraphicsSpawned;

        void Awake()
        {
            m_NetworkAvatarGuidState.GuidChanged += RegisterAvatar;
        }

        void RegisterAvatar(Guid guid)
        {
            // based on the Guid received, Avatar is fetched from AvatarRegistry
            if (!m_AvatarRegistry.TryGetAvatar(guid, out var avatar))
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

            m_Avatar = avatar;

            m_CharacterClassContainer.SetCharacterClass(avatar.CharacterClass);

            // spawn avatar graphics GameObject
            var graphicsGameObject = Instantiate(avatar.Graphics, m_GraphicsAnimator.transform);

            m_ClientCharacter.SetCharacterVisualization(graphicsGameObject.GetComponent<ClientCharacterVisualization>());

            m_GraphicsAnimator.Rebind();
            m_GraphicsAnimator.Update(0f);

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
