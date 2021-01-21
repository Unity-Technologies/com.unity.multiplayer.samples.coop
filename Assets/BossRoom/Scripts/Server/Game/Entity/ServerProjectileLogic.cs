using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MLAPI;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;

namespace BossRoom.Server
{

    public class ServerProjectileLogic : MLAPI.NetworkedBehaviour
    {
        private bool m_Started = false;

        [SerializeField]
        private ProjectileNetState m_NetState;

        [SerializeField]
        private SphereCollider m_OurCollider;

        private float m_MovementSpeed;

        private Vector3 m_StartPosition;

        private ActionDescription m_DrivingAction;

        private const int k_MaxCollisions = 4;
        private const float k_WallLingerSec = 2f; //time in seconds that arrows linger after hitting a target.
        private const float k_EnemyLingerSec = 0.2f; //time after hitting an enemy that we persist. 
        private Collider[] m_CollisionCache= new Collider[k_MaxCollisions];

        /// <summary>
        /// Time when we should destroy this arrow, in Time.time seconds. 
        /// </summary>
        private float m_DestroyAtSec;

        private int m_LayerMask;

        public override void NetworkStart(Stream stream)
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_Started = true;
            bool actionGotten = GameDataSource.s_Instance.ActionDataByType.TryGetValue(m_NetState.SourceAction.Value, out m_DrivingAction);
            if (!actionGotten)
            {
                enabled = false;
                throw new System.Exception("Projectile " + name + " couldn't find action " + m_NetState.SourceAction.Value + " in data");
            }

            m_MovementSpeed = m_DrivingAction.ProjectileSpeed_m_s;
            m_StartPosition = transform.position;
            m_DestroyAtSec = Time.fixedTime + (m_DrivingAction.Range / m_DrivingAction.ProjectileSpeed_m_s);

            m_LayerMask = LayerMask.GetMask(new string[]{"NPCs", "Default" });

            RefreshNetworkState();
        }

        private void FixedUpdate()
        {
            if (!m_Started) { return; } //don't do anything before NetworkStart has run.

            Vector3 displacement = transform.forward * (m_MovementSpeed * Time.fixedDeltaTime);
            transform.position += displacement;

            if (m_DestroyAtSec < Time.fixedTime )
            {
                //we've reached our range terminus.End of the road! Time to go away.
                Destroy(gameObject);
            }

            if( m_MovementSpeed > 0 )
            {
                DetectCollisions();
            }

            RefreshNetworkState();
        }

        private void RefreshNetworkState()
        {
            m_NetState.NetworkPosition.Value = transform.position;
            m_NetState.NetworkRotationY.Value = transform.eulerAngles.y;
            m_NetState.NetworkMovementSpeed.Value = m_MovementSpeed;
        }

        private void DetectCollisions()
        {
            Vector3 position = transform.localToWorldMatrix.MultiplyPoint(m_OurCollider.center);
            int numCollisions = Physics.OverlapSphereNonAlloc(position, m_OurCollider.radius, m_CollisionCache);
            for( int i = 0; i < numCollisions; i++ )
            {
                if( m_CollisionCache[i].gameObject.layer == LayerMask.NameToLayer("Default") )
                {
                    //hit a wall; leave it for a couple of seconds. 
                    m_MovementSpeed = 0;
                    m_DestroyAtSec = Time.fixedTime + k_WallLingerSec;
                    return;
                }

                if( m_CollisionCache[i].gameObject.layer == LayerMask.NameToLayer("NPCs") )
                {
                    //hit an enemy. We don't yet have a good way of visualizing this, so we just destroy ourselves quite quickly. 
                    m_DestroyAtSec = Time.fixedTime + 0.2f;

                    //all NPC layer entities should have one of these. 
                    var targetNetObj = m_CollisionCache[i].GetComponent<NetworkedObject>();
                    InvokeClientRpcOnEveryone(m_NetState.HitEnemy, targetNetObj.NetworkId);

                    targetNetObj.GetComponent<ServerCharacter>().ReceiveHP(null, -m_DrivingAction.Amount);
                    return;
                }
            }
        }
    }
}

