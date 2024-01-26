using System;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    [Serializable]
    public struct ProjectileInfo
    {
        [Tooltip("Prefab used for the projectile")]
        public GameObject ProjectilePrefab;
        [Tooltip("Projectile's speed in meters/second")]
        public float Speed_m_s;
        [Tooltip("Maximum range of the Projectile")]
        public float Range;
        [Tooltip("Damage of the Projectile on hit")]
        public int Damage;
        [Tooltip("Max number of enemies this projectile can hit before disappearing")]
        public int MaxVictims;
    }
}
