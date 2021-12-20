using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class DamageReceiver : NetworkBehaviour, IDamageable
    {
        public event Action<ServerCharacter, int> damageReceived;

        public event Action<Collision> collisionEntered;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public bool GodMode { get; set; }

        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            if (!GodMode)
            {
                damageReceived?.Invoke(inflicter, HP);
            }
        }

        public IDamageable.SpecialDamageFlags GetSpecialDamageFlags()
        {
            return IDamageable.SpecialDamageFlags.None;
        }

        public bool IsDamageable()
        {
            return m_NetworkLifeState.LifeState.Value == LifeState.Alive;
        }

        void OnCollisionEnter(Collision other)
        {
            collisionEntered?.Invoke(other);
        }
    }
}
