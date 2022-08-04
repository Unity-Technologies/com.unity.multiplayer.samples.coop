using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    [RequireComponent(typeof(NetworkCharacterState), typeof(NetworkLifeState))]
    public class ServerDropItemOnFaint : NetworkBehaviour
    {
        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        NetworkCharacterState m_NetworkCharacterState;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_NetworkLifeState.LifeState.OnValueChanged += OnValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            m_NetworkLifeState.LifeState.OnValueChanged -= OnValueChanged;
        }

        void OnValueChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue == LifeState.Fainted)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_NetworkCharacterState.heldNetworkObject.Value, out var heavyNetworkObject))
                {
                    heavyNetworkObject.transform.SetParent(null);
                }
                m_NetworkCharacterState.heldNetworkObject.Value = 0;
            }
        }
    }
}
