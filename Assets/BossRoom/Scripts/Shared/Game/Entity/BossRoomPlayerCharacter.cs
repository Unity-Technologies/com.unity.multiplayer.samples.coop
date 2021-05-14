using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour that will be present on every player character while in-game (specifically BossRoom scene).
    /// Therefore, this NetworkObject will only contain other NetworkBehaviour components that are pertinent only to the
    /// game. This NetworkObject is spawned by the server, and the owner of this NetworkObject will be its corresponding
    /// <see cref="BossRoom.BossRoomPlayer"/> NetworkObject. A reference to this NetworkObject's owner is assigned during
    /// NetworkStart(), and if not present (this can happen during a late-join scenario), it will wait until an event
    /// is fired by the owning BossRoomPlayer.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class BossRoomPlayerCharacter : NetworkBehaviour
    {
        [SerializeField]
        BossRoomPlayerRuntimeCollection m_BossRoomPlayers;

        [SerializeField]
        BossRoomPlayerCharacterRuntimeCollection m_BossRoomPlayerCharacters;

        [SerializeField]
        NetworkBehaviourLookup m_NetworkBehaviourLookup;

        BossRoomPlayer m_BossRoomPlayer;

        public event Action BossRoomPlayerNetworkReadied;

        public BossRoomPlayer BossRoomPlayer => m_BossRoomPlayer;

        public override void NetworkStart()
        {
            gameObject.name = "BossRoomPlayerCharacter" + OwnerClientId;

            TryGetPlayer();

            if (!m_BossRoomPlayer)
            {
                m_BossRoomPlayers.ListChanged += TryGetPlayer;
            }

            m_BossRoomPlayerCharacters.Add(this);
        }

        void TryGetPlayer()
        {
            if (!m_BossRoomPlayer && m_BossRoomPlayers.TryGetPlayer(OwnerClientId, out m_BossRoomPlayer))
            {
                BossRoomPlayerNetworkReadied?.Invoke();
            }
        }

        public bool TryGetNetworkBehaviour<T>(out T networkBehaviourType) where T : NetworkBehaviour
        {
            return m_NetworkBehaviourLookup.TryGetNetworkBehaviour(out networkBehaviourType);
        }

        void OnDestroy()
        {
            m_BossRoomPlayers.ListChanged -= TryGetPlayer;

            m_BossRoomPlayerCharacters.Remove(this);
        }
    }
}
