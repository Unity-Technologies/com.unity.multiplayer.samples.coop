using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Wrapper class for direct references to components relevant to physics.
    /// Each instance of a PhysicsWrapper is registered to a static dictionary, indexed by the NetworkObject's ID.
    /// </summary>
    /// <remarks>
    /// The root GameObject of PCs & NPCs is not the object which will move through the world, so other classes will
    /// need a quick reference to a PC's/NPC's in-game position.
    /// </remarks>
    public class PhysicsWrapper : NetworkBehaviour
    {
        static Dictionary<ulong, PhysicsWrapper> m_PhysicsWrappers = new Dictionary<ulong, PhysicsWrapper>();

        [SerializeField]
        Transform m_Transform;

        public Transform Transform => m_Transform;

        [SerializeField]
        Collider m_DamageCollider;

        public Collider DamageCollider => m_DamageCollider;

        ulong m_NetworkObjectID;

        public override void OnNetworkSpawn()
        {
            m_PhysicsWrappers.Add(NetworkObjectId, this);

            m_NetworkObjectID = NetworkObjectId;
        }

        public override void OnNetworkDespawn()
        {
            RemovePhysicsWrapper();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RemovePhysicsWrapper();
        }

        void RemovePhysicsWrapper()
        {
            m_PhysicsWrappers.Remove(m_NetworkObjectID);
        }

        public static bool TryGetPhysicsWrapper(ulong networkObjectID, out PhysicsWrapper physicsWrapper)
        {
            return m_PhysicsWrappers.TryGetValue(networkObjectID, out physicsWrapper);
        }
    }
}
