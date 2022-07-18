using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

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

        [Inject]
        IPublisher<LifeStateChangedEventMessage> m_Publisher;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_NameState = GetComponent<NetworkNameState>();
                m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;

                var gameState = FindObjectOfType<BossRoomState>();
                if (gameState != null)
                {
                    gameState.Container.Inject(this);
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
