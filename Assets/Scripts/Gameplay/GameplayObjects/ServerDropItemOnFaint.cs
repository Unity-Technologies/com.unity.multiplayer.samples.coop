using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    [RequireComponent(typeof(NetworkPickUpState), typeof(NetworkLifeState))]
    public class ServerDropItemOnFaint : NetworkBehaviour
    {
        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        NetworkPickUpState m_NetworkPickUpState;

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
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_NetworkPickUpState.heldObject.Value, out var heavyNetworkObject))
                {
                    m_NetworkPickUpState.heldObject.Value = 0;
                    heavyNetworkObject.transform.SetParent(null);
                }
            }
        }
    }
}
