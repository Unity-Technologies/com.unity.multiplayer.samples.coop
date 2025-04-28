using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class DamageReceiver : NetworkBehaviour, IDamageable
    {
        public event Action<ServerCharacter, int> DamageReceived;

        public event Action<Collision> CollisionEntered;

        public event Func<int> GetTotalDamageFunc;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public void ReceiveHitPoints(ServerCharacter inflicter, int hitPoints)
        {
            if (IsDamageable())
            {
                DamageReceived?.Invoke(inflicter, hitPoints);
            }
        }

        public int GetTotalDamage()
        {
            if (!IsDamageable())
            {
                return 0;
            }

            return GetTotalDamageFunc?.Invoke() ?? 0;
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
            CollisionEntered?.Invoke(other);
        }
    }
}
