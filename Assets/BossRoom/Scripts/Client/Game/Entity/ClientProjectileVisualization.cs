using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Visual
{
    public class ClientProjectileVisualization : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Explosion prefab used when projectile hits enemy. This should have a fixed duration.")]
        private SpecialFXGraphic m_OnHitParticlePrefab;

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

        void OnDestroy()
        {
            if( m_NetState != null )
            {
                m_NetState.HitEnemyEvent -= OnEnemyHit;
            }
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

            NetworkObject targetNetObject;
            if (NetworkSpawnManager.SpawnedObjects.TryGetValue(enemyId, out targetNetObject))
            {
                if (m_OnHitParticlePrefab)
                {
                    // show an impact graphic
                    Instantiate(m_OnHitParticlePrefab.gameObject, transform.position, transform.rotation);
                }

                var clientChar = targetNetObject.GetComponent<Client.ClientCharacter>();
                if(clientChar)
                {
                    clientChar.ChildVizObject.OurAnimator.SetTrigger(ActionFX.k_DefaultHitReact);
                }
            }
        }
    }


}

