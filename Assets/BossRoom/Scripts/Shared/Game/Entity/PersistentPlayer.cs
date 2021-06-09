using System;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour that represents a player connection and is the "Default Player Prefab" according to MLAPI. This
    /// NetworkBehaviour will contain several other NetworkBehaviours that should persist throughout the duration of
    /// this connection, meaning it will persist between scenes.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PersistentPlayer : NetworkBehaviour
    {
        [SerializeField]
        PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public override void NetworkStart()
        {
            gameObject.name = "PersistentPlayer" + OwnerClientId;

            // Note that this is done here on NetworkStart in case this NetworkBehaviour's properties are accessed
            // when this element is added to the runtime collection. If this was done in OnEnable() there is a chance
            // that OwnerClientID could be its default value (0).
            m_PersistentPlayerRuntimeCollection.Add(this);
        }

        void OnDestroy()
        {
            m_PersistentPlayerRuntimeCollection.Remove(this);
        }
    }
}
