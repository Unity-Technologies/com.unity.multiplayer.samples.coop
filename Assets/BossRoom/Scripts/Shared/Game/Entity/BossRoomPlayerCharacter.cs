using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour that will be present on every player character while in-game (specifically BossRoom scene).
    /// Therefore, this NetworkObject will only contain other NetworkBehaviour components that are pertinent only to the
    /// game. This NetworkObject is spawned by the server, and the owner of this NetworkObject will be its corresponding
    /// <see cref="BossRoom.BossRoomPlayer"/> NetworkObject. A reference to this NetworkObject's owner is assigned
    /// during NetworkStart(), and if not present (this can happen during a late-join scenario), it will wait until an
    /// event is fired by the owning BossRoomPlayer.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class BossRoomPlayerCharacter : NetworkBehaviour
    {
        [SerializeField]
        BossRoomPlayerRuntimeCollection m_BossRoomPlayers;

        [SerializeField]
        BossRoomPlayerCharacterRuntimeCollection m_BossRoomPlayerCharacters;

        BossRoomPlayer m_BossRoomPlayer;

        public BossRoomPlayer BossRoomPlayer => m_BossRoomPlayer;

        /// <summary>
        /// The callback to invoke once this BossRoomPlayerCharacter's associated BossRoomPlayer is set.
        /// This callback is ran on both server and clients.
        /// </summary>
        public event Action BossRoomPlayerCharacterNetworkReadied;

        public override void NetworkStart()
        {
            gameObject.name = "BossRoomPlayerCharacter" + OwnerClientId;

            if (!TryGetPlayer(out m_BossRoomPlayer))
            {
                // owning BossRoomPlayer not present yet; this can happen in a late-join scenario
                // wait until NetworkStart() is fired on matching BossRoomPlayer
                BossRoomPlayer.BossRoomPlayerNetworkStarted += TryGetPlayer;
            }
        }

        void TryGetPlayer()
        {
            if (TryGetPlayer(out m_BossRoomPlayer))
            {
                // matching BossRoomPlayer found; unsubscribe from BossRoomPlayer callback
                BossRoomPlayer.BossRoomPlayerNetworkStarted -= TryGetPlayer;
            }
        }

        bool TryGetPlayer(out BossRoomPlayer bossRoomPlayer)
        {
            if (m_BossRoomPlayers.TryGetPlayer(OwnerClientId, out bossRoomPlayer))
            {
                // matching BossRoomPlayer found; invoke callback for any behaviours dependent on BossRoomPlayer data
                BossRoomPlayerCharacterNetworkReadied?.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        void OnDestroy()
        {
            BossRoomPlayer.BossRoomPlayerNetworkStarted -= TryGetPlayer;

            m_BossRoomPlayerCharacters.Remove(this);
        }
    }
}
