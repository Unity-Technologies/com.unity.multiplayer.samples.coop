using System;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
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
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        public event Action<GameObject> AvatarGraphicsSpawned;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InstantiateAvatar();
        }

        void InstantiateAvatar()
        {
            if (m_ClientCharacter.ChildVizObject)
            {
                // we may receive a NetworkVariable's OnValueChanged callback more than once as a client
                // this makes sure we don't spawn a duplicate graphics GameObject
                return;
            }

            // spawn avatar graphics GameObject
            var graphicsGameObject = Instantiate(m_NetworkAvatarGuidState.RegisteredAvatar.Graphics, transform);

            m_ClientCharacter.SetCharacterVisualization(graphicsGameObject.GetComponent<ClientCharacterVisualization>());

            AvatarGraphicsSpawned?.Invoke(graphicsGameObject);
        }
    }
}
