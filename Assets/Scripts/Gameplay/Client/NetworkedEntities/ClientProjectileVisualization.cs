using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class ClientProjectileVisualization : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Explosion prefab used when projectile hits enemy. This should have a fixed duration.")]
        SpecialFXGraphic m_OnHitParticlePrefab;

        [SerializeField]
        TrailRenderer m_TrailRenderer;

        NetworkProjectileState m_NetState;

        Transform m_Parent;

        const float k_LerpTime = 0.1f;

        PositionLerper m_PositionLerper;

        public override void OnNetworkSpawn()
        {
            if (!IsClient || transform.parent == null)
            {
                enabled = false;
                return;
            }

            m_TrailRenderer.Clear();

            m_Parent = transform.parent;
            transform.parent = null;
            m_NetState = m_Parent.GetComponent<NetworkProjectileState>();
            m_NetState.HitEnemyEvent += OnEnemyHit;

            m_PositionLerper = new PositionLerper(m_Parent.position, k_LerpTime);
            transform.rotation = m_Parent.transform.rotation;
        }

        public override void OnNetworkDespawn()
        {
            m_TrailRenderer.Clear();

            if (m_NetState != null)
            {
                transform.parent = m_Parent;
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

            // One thing to note: this graphics GameObject is detached from its parent on OnNetworkSpawn. On the host,
            // the m_Parent Transform is translated via ServerProjectileLogic's FixedUpdate method. On all other
            // clients, m_Parent's NetworkTransform handles syncing and interpolating the m_Parent Transform. Thus, to
            // eliminate any visual jitter on the host, this GameObject is positionally smoothed over time. On all other
            // clients, no positional smoothing is required, since m_Parent's NetworkTransform will perform
            // positional interpolation on its Update method, and so this position is simply matched 1:1 with m_Parent.

            if (IsHost)
            {
                transform.position = m_PositionLerper.LerpPosition(transform.position,
                    m_Parent.transform.position);
            }
            else
            {
                transform.position = m_Parent.position;
            }
        }

        void OnEnemyHit(ulong enemyId)
        {
            //in the future we could do quite fancy things, like deparenting the Graphics Arrow and parenting it to the target.
            //For the moment we play some particles (optionally), and cause the target to animate a hit-react.

            NetworkObject targetNetObject;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemyId, out targetNetObject))
            {
                if (m_OnHitParticlePrefab)
                {
                    // show an impact graphic
                    Instantiate(m_OnHitParticlePrefab.gameObject, transform.position, transform.rotation);
                }
            }
        }
    }
}
