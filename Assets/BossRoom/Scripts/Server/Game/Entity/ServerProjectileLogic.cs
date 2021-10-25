using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using BossRoom.Scripts.Shared.Net.NetworkObjectPool;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{

    public class ServerProjectileLogic : NetworkBehaviour
    {
        private bool m_Started = false;

        [SerializeField]
        private NetworkProjectileState m_NetState;

        [SerializeField]
        private SphereCollider m_OurCollider;

        /// <summary>
        /// The character that created us. Can be 0 to signal that we were created generically by the server.
        /// </summary>
        private ulong m_SpawnerId;

        /// <summary>
        /// The data for our projectile. Indicates speed, damage, etc.
        /// </summary>
        private ActionDescription.ProjectileInfo m_ProjectileInfo;

        private const int k_MaxCollisions = 4;
        private const float k_WallLingerSec = 2f; //time in seconds that arrows linger after hitting a target.
        private const float k_EnemyLingerSec = 0.2f; //time after hitting an enemy that we persist.
        private Collider[] m_CollisionCache = new Collider[k_MaxCollisions];

        /// <summary>
        /// Time when we should destroy this arrow, in Time.time seconds.
        /// </summary>
        private float m_DestroyAtSec;

        private int m_CollisionMask;  //mask containing everything we test for while moving
        private int m_BlockerMask;    //physics mask for things that block the arrow's flight.
        private int m_NPCLayer;

        /// <summary>
        /// List of everyone we've hit and dealt damage to.
        /// </summary>
        /// <remarks>
        /// Note that it's possible for entries in this list to become null if they're Destroyed post-impact.
        /// But that's fine by us! We use <c>m_HitTargets.Count</c> to tell us how many total enemies we've hit,
        /// so those nulls still count as hits.
        /// </remarks>
        private List<GameObject> m_HitTargets = new List<GameObject>();

        /// <summary>
        /// Are we done moving?
        /// </summary>
        private bool m_IsDead;

        /// <summary>
        /// Set everything up based on provided projectile information.
        /// (Note that this is called before OnNetworkSpawn(), so don't try to do any network stuff here.)
        /// </summary>
        public void Initialize(ulong creatorsNetworkObjectId, in ActionDescription.ProjectileInfo projectileInfo)
        {
            m_SpawnerId = creatorsNetworkObjectId;
            m_ProjectileInfo = projectileInfo;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            m_Started = true;

            m_HitTargets = new List<GameObject>();
            m_IsDead = false;

            m_DestroyAtSec = Time.fixedTime + (m_ProjectileInfo.Range / m_ProjectileInfo.Speed_m_s);

            m_CollisionMask = LayerMask.GetMask(new[] { "NPCs", "Default", "Ground" });
            m_BlockerMask = LayerMask.GetMask(new[] { "Default", "Ground" });
            m_NPCLayer = LayerMask.NameToLayer("NPCs");
        }

        private void FixedUpdate()
        {
            if (!m_Started) { return; } //don't do anything before OnNetworkSpawn has run.

            Vector3 displacement = transform.forward * (m_ProjectileInfo.Speed_m_s * Time.fixedDeltaTime);
            transform.position += displacement;

            if (m_DestroyAtSec < Time.fixedTime)
            {
                // Time to return to the pool from whence it came.
                NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
                networkObject.Despawn();
            }

            if (!m_IsDead)
            {
                DetectCollisions();
            }
        }

        private void DetectCollisions()
        {
            Vector3 position = transform.localToWorldMatrix.MultiplyPoint(m_OurCollider.center);
            int numCollisions = Physics.OverlapSphereNonAlloc(position, m_OurCollider.radius, m_CollisionCache, m_CollisionMask);
            for (int i = 0; i < numCollisions; i++)
            {
                int layerTest = 1 << m_CollisionCache[i].gameObject.layer;
                if ((layerTest & m_BlockerMask) != 0)
                {
                    //hit a wall; leave it for a couple of seconds.
                    m_ProjectileInfo.Speed_m_s = 0;
                    m_IsDead = true;
                    m_DestroyAtSec = Time.fixedTime + k_WallLingerSec;
                    return;
                }

                if (m_CollisionCache[i].gameObject.layer == m_NPCLayer && !m_HitTargets.Contains(m_CollisionCache[i].gameObject))
                {
                    m_HitTargets.Add(m_CollisionCache[i].gameObject);

                    if (m_HitTargets.Count >= m_ProjectileInfo.MaxVictims)
                    {
                        // we've hit all the enemies we're allowed to! So we're done
                        m_DestroyAtSec = Time.fixedTime + k_EnemyLingerSec;
                        m_IsDead = true;
                    }

                    //all NPC layer entities should have one of these.
                    var targetNetObj = m_CollisionCache[i].GetComponentInParent<NetworkObject>();
                    if (targetNetObj)
                    {
                        m_NetState.RecvHitEnemyClientRPC(targetNetObj.NetworkObjectId);

                        //retrieve the person that created us, if he's still around.
                        NetworkObject spawnerNet;
                        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_SpawnerId, out spawnerNet);
                        ServerCharacter spawnerObj = spawnerNet != null ? spawnerNet.GetComponent<ServerCharacter>() : null;

                        m_CollisionCache[i].GetComponent<IDamageable>().ReceiveHP(spawnerObj, -m_ProjectileInfo.Damage);
                    }

                    if (m_IsDead)
                        return; // don't keep examining collisions since we can't damage anybody else
                }
            }
        }
    }
}

