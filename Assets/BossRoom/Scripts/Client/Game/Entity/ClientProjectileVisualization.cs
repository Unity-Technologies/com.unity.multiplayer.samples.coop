using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

namespace BossRoom.Visual
{
    public class ClientProjectileVisualization : NetworkedBehaviour
    {
        NetworkProjectileState m_NetState;
        Transform m_Parent;

        private const float k_MaxTurnRateDegreesSecond = 280;

        public override void NetworkStart()
        {
            if (!IsClient || transform.parent == null)
            {
                enabled = false;
                return;
            }

            m_Parent = transform.parent;
            transform.parent = null;
            m_NetState = m_Parent.GetComponent<NetworkProjectileState>();
            m_NetState.HitEnemyEvent += OnEnemyHit;
        }

        void Update()
        {
            if( m_Parent == null )
            {
                Destroy(gameObject);
                return;
            }

            VisualUtils.SmoothMove(transform, m_Parent.transform, Time.deltaTime,
                m_NetState.NetworkMovementSpeed.Value, k_MaxTurnRateDegreesSecond);
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

