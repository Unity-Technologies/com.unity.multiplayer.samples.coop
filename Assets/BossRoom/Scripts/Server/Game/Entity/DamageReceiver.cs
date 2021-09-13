using System;
using MLAPI;
using UnityEngine;

namespace BossRoom.Server
{
    public class DamageReceiver : NetworkBehaviour, IDamageable
    {
        public event Action<ServerCharacter, int> damageReceived;

        public event Action<Collision> collisionEntered;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            damageReceived?.Invoke(inflicter, HP);
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
