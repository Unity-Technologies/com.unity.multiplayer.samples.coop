using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Utils;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// NetworkBehaviour that represents a player connection and is the "Default Player Prefab" inside Netcode for
    /// GameObjects' (Netcode) NetworkManager. This NetworkBehaviour will contain several other NetworkBehaviours that
    /// should persist throughout the duration of this connection, meaning it will persist between scenes.
    /// </summary>
    /// <remarks>
    /// It is not necessary to explicitly mark this as a DontDestroyOnLoad object as Netcode will handle migrating this
    /// Player object between scene loads.
    /// </remarks>
    [RequireComponent(typeof(NetworkObject))]
    public class PersistentPlayer : NetworkBehaviour
    {
        [SerializeField]
        PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;

        [SerializeField]
        NetworkNameState m_NetworkNameState;

        [SerializeField]
        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        public NetworkNameState NetworkNameState => m_NetworkNameState;

        public NetworkAvatarGuidState NetworkAvatarGuidState => m_NetworkAvatarGuidState;

        public override void OnNetworkSpawn()
        {
            gameObject.name = "PersistentPlayer" + OwnerClientId;

            // Note that this is done here on OnNetworkSpawn in case this NetworkBehaviour's properties are accessed
            // when this element is added to the runtime collection. If this was done in OnEnable() there is a chance
            // that OwnerClientID could be its default value (0).
            m_PersistentPlayerRuntimeCollection.Add(this);
            if (IsServer)
            {
                var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    var playerData = sessionPlayerData.Value;
                    m_NetworkNameState.Name.Value = playerData.PlayerName;
                    if (playerData.HasCharacterSpawned)
                    {
                        m_NetworkAvatarGuidState.AvatarGuid.Value = playerData.AvatarNetworkGuid;
                    }
                    else
                    {
                        m_NetworkAvatarGuidState.SetRandomAvatar();
                        playerData.AvatarNetworkGuid = m_NetworkAvatarGuidState.AvatarGuid.Value;
                        SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                    }
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RemovePersistentPlayer();
        }

        public override void OnNetworkDespawn()
        {
            RemovePersistentPlayer();
        }

        void RemovePersistentPlayer()
        {
            m_PersistentPlayerRuntimeCollection.Remove(this);
            if (IsServer)
            {
                var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    var playerData = sessionPlayerData.Value;
                    playerData.PlayerName = m_NetworkNameState.Name.Value;
                    playerData.AvatarNetworkGuid = m_NetworkAvatarGuidState.AvatarGuid.Value;
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                }
            }
        }
    }
}
