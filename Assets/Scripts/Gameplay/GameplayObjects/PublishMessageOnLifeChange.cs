using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server-only component which publishes a message once the LifeState changes.
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState))]
    public class PublishMessageOnLifeChange : NetworkBehaviour
    {
        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        string m_CharacterName;

        [SerializeField]
        CharacterClassContainer m_CharacterClass;

        NetworkNameState m_NameState;

        IPublisher<LifeStateChangedEventMessage> m_Publisher;

        [Inject]
        void InjectDependencies(IPublisher<LifeStateChangedEventMessage> publisher)
        {
            m_Publisher = publisher;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_NameState = GetComponent<NetworkNameState>();
                m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;

                var gameState = FindObjectOfType<ServerBossRoomState>();
                if (gameState != null)
                {
                    gameState.Scope.InjectIn(this);
                }
            }
        }

        void OnLifeStateChanged(LifeState previousState, LifeState newState)
        {
            m_Publisher.Publish(new LifeStateChangedEventMessage()
            {
                CharacterName = m_NameState != null ? m_NameState.Name.Value : (FixedPlayerName)m_CharacterName,
                CharacterType = m_CharacterClass.CharacterClass.CharacterType,
                NewLifeState = newState
            });
        }
    }
}
