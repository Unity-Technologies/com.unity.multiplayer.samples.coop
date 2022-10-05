using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.GameState;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Server-only component which publishes a message once the LifeState changes.
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState), typeof(ServerCharacter))]
    public class PublishMessageOnLifeChange : NetworkBehaviour
    {
        NetworkLifeState m_NetworkLifeState;
        ServerCharacter m_ServerCharacter;

        [SerializeField]
        string m_CharacterName;

        NetworkNameState m_NameState;

        [Inject]
        IPublisher<LifeStateChangedEventMessage> m_Publisher;

        void Awake()
        {
            m_NetworkLifeState = GetComponent<NetworkLifeState>();
            m_ServerCharacter = GetComponent<ServerCharacter>();
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
                    gameState.Container.Inject(this);
                }
            }
        }

        void OnLifeStateChanged(LifeState previousState, LifeState newState)
        {
            m_Publisher.Publish(new LifeStateChangedEventMessage()
            {
                CharacterName = m_NameState != null ? m_NameState.Name.Value : (FixedPlayerName)m_CharacterName,
                CharacterType = m_ServerCharacter.CharacterClass.CharacterType,
                NewLifeState = newState
            });
        }
    }
}
