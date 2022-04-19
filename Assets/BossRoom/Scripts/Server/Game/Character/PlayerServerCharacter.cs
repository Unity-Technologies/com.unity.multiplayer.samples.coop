using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Attached to the player-characters' prefab, this maintains a list of active ServerCharacter objects for players.
    /// </summary>
    /// <remarks>
    /// This is an optimization. In server code you can already get a list of players' ServerCharacters by
    /// iterating over the active connections and calling GetComponent() on their PlayerObject. But we need
    /// to iterate over all players quite often -- the monsters' IdleAIState does so in every Update() --
    /// and all those GetComponent() calls add up! So this optimization lets us iterate without calling
    /// GetComponent(). This will be refactored with a ScriptableObject-based approach on player collection.
    /// </remarks>
    [RequireComponent(typeof(ServerCharacter))]
    public class PlayerServerCharacter : NetworkBehaviour
    {
        static List<ServerCharacter> s_ActivePlayers = new List<ServerCharacter>();

        [SerializeField]
        ServerCharacter m_CachedServerCharacter;

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
                s_ActivePlayers.Add(m_CachedServerCharacter);
                m_NameState = GetComponent<NetworkNameState>();
                m_CachedServerCharacter.NetState.NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            }
            else
            {
                enabled = false;
            }

        }

        void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (m_NameState != null)
            {
                m_Publisher.Publish(new LifeStateChangedEventMessage()
                {
                    CharacterName = m_NameState.Name.Value,
                    CharacterType = m_CachedServerCharacter.NetState.CharacterClass.CharacterType,
                    NewLifeState = newValue
                });
            }
        }

        void OnDisable()
        {
            s_ActivePlayers.Remove(m_CachedServerCharacter);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                var movementTransform = m_CachedServerCharacter.Movement.transform;
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    var playerData = sessionPlayerData.Value;
                    playerData.PlayerPosition = movementTransform.position;
                    playerData.PlayerRotation = movementTransform.rotation;
                    playerData.CurrentHitPoints = m_CachedServerCharacter.NetState.HitPoints;
                    playerData.HasCharacterSpawned = true;
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                }
            }
        }

        /// <summary>
        /// Returns a list of all active players' ServerCharacters. Treat the list as read-only!
        /// The list will be empty on the client.
        /// </summary>
        public static List<ServerCharacter> GetPlayerServerCharacters()
        {
            return s_ActivePlayers;
        }

        /// <summary>
        /// Returns the ServerCharacter owned by a specific client. Always returns null on the client.
        /// </summary>
        /// <param name="ownerClientId"></param>
        /// <returns>The ServerCharacter owned by the client, or null if no ServerCharacter is found</returns>
        public static ServerCharacter GetPlayerServerCharacter(ulong ownerClientId)
        {
            foreach (var playerServerCharacter in s_ActivePlayers)
            {
                if (playerServerCharacter.OwnerClientId == ownerClientId)
                {
                    return playerServerCharacter;
                }
            }
            Debug.LogError($"PlayerServerCharacter owned by client {ownerClientId} not found");
            return null;
        }
    }
}
