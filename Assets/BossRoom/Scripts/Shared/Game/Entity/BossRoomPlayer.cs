using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour that represent a player connection and is the "Default Player Prefab" according to MLAPI. This
    /// NetworkBehaviour will contain several other NetworkBehaviours that should persist throughout the duration of
    /// this connection, meaning it will persist between scenes.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class BossRoomPlayer : NetworkBehaviour
    {
        [SerializeField]
        BossRoomPlayerRuntimeCollection m_BossRoomPlayerRuntimeCollection;

        [SerializeField]
        NetworkBehaviourLookup m_NetworkBehaviourLookup;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public override void NetworkStart()
        {
            gameObject.name = "BossRoomPlayer" + OwnerClientId;

            // Note that this is done here on NetworkStart in case this NetworkBehaviour's properties are looked up
            // when this element is added to the runtime collection. If this was done in OnEnable() there is a chance
            // that OwnerClientID could be its default value (0).
            m_BossRoomPlayerRuntimeCollection.Add(this);
        }

        public bool TryGetNetworkBehaviour<T>(out T networkBehaviourType) where T : NetworkBehaviour
        {
            return m_NetworkBehaviourLookup.TryGetNetworkBehaviour(out networkBehaviourType);
        }

        void OnDestroy()
        {
            m_BossRoomPlayerRuntimeCollection.Remove(this);
        }
    }
}
