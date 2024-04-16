using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

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
        [SerializeField]
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        bool m_AvatarInstantiated;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
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
            Instantiate(m_NetworkAvatarGuidState.RegisteredAvatar.Graphics, Animator.transform);

            Animator.Rebind();

            m_AvatarInstantiated = true;
        }
    }
}
