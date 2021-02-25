using MLAPI;
using UnityEngine;

namespace BossRoom.Visual
{
    public class ClientProjectileVisualization : NetworkedBehaviour
    {
        [SerializeField]
        private GameObject m_OnHitParticlePrefab;

        NetworkProjectileState m_NetState;
        Transform m_Parent;

        private const float k_MaxTurnRateDegreesSecond = 280;

        private float m_SmoothedSpeed;

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
            if (m_Parent == null)
            {
                Destroy(gameObject);
                return;
            }

            VisualUtils.SmoothMove(transform, m_Parent.transform, Time.deltaTime, ref m_SmoothedSpeed, k_MaxTurnRateDegreesSecond);
        }

        private void OnEnemyHit(ulong enemyId)
        {
            //in the future we could do quite fancy things, like deparenting the Graphics Arrow and parenting it to the target.
            //For the moment we play some particles (optionally), and cause the target to animate a hit-react.

            NetworkedObject targetNetObject;
            if (MLAPI.Spawning.SpawnManager.SpawnedObjects.TryGetValue(enemyId, out targetNetObject))
            {
                if (m_OnHitParticlePrefab)
                {
                    // show an impact graphic
                    Instantiate(m_OnHitParticlePrefab, transform.position, transform.rotation);
                }

                ClientCharacterVisualization charViz = targetNetObject.GetComponent<Client.ClientCharacter>().ChildVizObject;
                charViz.OurAnimator.SetTrigger("HitReact1");
            }
        }
    }


}

