using System;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Client-side component that awaits a state change on an avatar's Guid, and fetches matching Avatar from the
    /// AvatarRegistry, if possible. Once fetched, the Graphics GameObject is spawned.
    /// </summary>
    [RequireComponent(typeof(NetworkAvatarGuidState))]
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientAvatarGuidHandler : MonoBehaviour
    {
        [SerializeField]
        Animator m_GraphicsAnimator;

        [SerializeField]
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        public Animator graphicsAnimator => m_GraphicsAnimator;

        public event Action<GameObject> AvatarGraphicsSpawned;
        void Awake()
        {
            GetComponent<NetcodeHooks>().OnNetworkSpawnHook += OnSpawn;
        }

        void OnDestroy()
        {
            GetComponent<NetcodeHooks>().OnNetworkSpawnHook -= OnSpawn;
        }
        public void OnSpawn()
        {
            InstantiateAvatar();
        }

        void InstantiateAvatar()
        {
            if (m_GraphicsAnimator.transform.childCount > 0)
            {
                // we may receive a NetworkVariable's OnValueChanged callback more than once as a client
                // this makes sure we don't spawn a duplicate graphics GameObject
                return;
            }

            // spawn avatar graphics GameObject
            Instantiate(m_NetworkAvatarGuidState.RegisteredAvatar.Graphics, m_GraphicsAnimator.transform);

            m_GraphicsAnimator.Rebind();
            m_GraphicsAnimator.Update(0f);

            AvatarGraphicsSpawned?.Invoke(m_GraphicsAnimator.gameObject);
        }
    }
}
