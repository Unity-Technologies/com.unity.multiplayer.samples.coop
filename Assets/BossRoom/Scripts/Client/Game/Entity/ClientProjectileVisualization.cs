using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

namespace BossRoom.Visual
{
    public class ClientProjectileVisualization : NetworkedBehaviour
    {
        ProjectileNetState m_NetState;
        Transform m_Parent;

        public override void NetworkStart()
        {
            if (!IsClient || transform.parent == null)
            {
                enabled = false;
                return;
            }

            m_Parent = transform.parent;
            transform.parent = null;
            m_NetState = m_Parent.GetComponent<ProjectileNetState>();
            m_NetState.HitEnemyEvent += OnEnemyHit;
        }

        void Update()
        {
            if( m_Parent == null )
            {
                Destroy(gameObject);
                return;
            }

            ClientCharacterVisualization.SmoothMove(transform, m_Parent.transform, Time.deltaTime, m_NetState.NetworkMovementSpeed.Value, 280);
        }

        private void OnEnemyHit(ulong enemyId)
        {
            //in the future we could do quite fancy things, like deparenting the Graphics Arrow and parenting it to the target.
            //For the moment we just play the hitreact, however.

            NetworkedObject targetNetObject;
            if( MLAPI.Spawning.SpawnManager.SpawnedObjects.TryGetValue(enemyId, out targetNetObject) )
            {
                ClientCharacterVisualization charViz = targetNetObject.GetComponent<Client.ClientCharacter>().ChildVizObject;
                charViz.OurAnimator.SetTrigger("HitReact1");
            }
        }
    }


}

