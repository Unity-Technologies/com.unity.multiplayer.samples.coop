using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Connection;

namespace BossRoom.Server
{
    /// <summary>
    /// Component responsible for spawning prefab clones in waves on the server.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ServerWaveSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        // amount of hits it takes to break any spawner
        [SerializeField]
        IntVariable m_MaxHP;

        // networked object that will be spawned in waves
        [SerializeField]
        NetworkObject m_NetworkedPrefab;

        // cache reference to our own transform
        Transform m_Transform;

        // track wave index and reset once all waves are complete
        int m_WaveIndex;

        // keep reference to our wave spawning coroutine
        Coroutine m_WaveSpawning;

        // cache array of RaycastHit as it will be reused for player visibility
        RaycastHit[] m_Hit;

        [Tooltip("Select which layers will block visibility.")]
        [SerializeField]
        LayerMask m_BlockingMask;

        [Tooltip("Time between player distance & visibility scans, in seconds.")]
        [SerializeField]
        float m_PlayerProximityValidationTimestep;

        [Header("Wave parameters")]
        [Tooltip("Total number of waves.")]
        [SerializeField]
        int m_NumberOfWaves;
        [Tooltip("Number of spawns per wave.")]
        [SerializeField]
        int m_SpawnsPerWave;
        [Tooltip("Time between individual spawns, in seconds.")]
        [SerializeField]
        float m_TimeBetweenSpawns;
        [Tooltip("Time between waves, in seconds.")]
        [SerializeField]
        float m_TimeBetweenWaves;
        [Tooltip("Once last wave is spawned, the spawner waits this long to restart wave spawns, in seconds.")]
        [SerializeField]
        float m_RestartDelay;
        [Tooltip("A player must be withing this distance to commence first wave spawn.")]
        [SerializeField]
        float m_ProximityDistance;
        [Tooltip("After being broken, the spawner waits this long to restart wave spawns, in seconds.")]
        [SerializeField]
        float m_DormantCooldown;

        void Awake()
        {
            m_Transform = transform;
        }

        public override void NetworkStart()
        {
            base.NetworkStart();

            if (!IsServer)
            {
                enabled = false;
                return;
            }

            ReviveSpawner();
            m_Hit = new RaycastHit[1];
            StartCoroutine(ValidatePlayersProximity(StartWaveSpawning));
        }

        /// <summary>
        /// Coroutine for continually validating proximity to players and invoking an action when any is near.
        /// </summary>
        /// <param name="validationAction"></param>
        /// <returns></returns>
        IEnumerator ValidatePlayersProximity(System.Action validationAction)
        {
            while (true)
            {
                if (m_NetworkHealthState.HitPoints.Value <= 0)
                {
                    yield return new WaitForSeconds(m_DormantCooldown);
                    ReviveSpawner();
                }

                if (m_WaveSpawning == null)
                {
                    if (IsAnyPlayerNearbyAndVisible())
                    {
                        validationAction();
                    }
                }
                else
                {
                    // do nothing, a wave spawning routine is currently underway
                }

                yield return new WaitForSeconds(m_PlayerProximityValidationTimestep);
            }
        }

        void StartWaveSpawning()
        {
            StopWaveSpawning();

            m_WaveSpawning = StartCoroutine(SpawnWaves());
        }

        void StopWaveSpawning()
        {
            if (m_WaveSpawning != null)
            {
                StopCoroutine(m_WaveSpawning);
            }
            m_WaveSpawning = null;
        }

        /// <summary>
        /// Coroutine for spawning prefabs clones in waves, waiting a duration before spawning a new wave.
        /// Once all waves are completed, it waits a restart time before termination.
        /// </summary>
        /// <returns></returns>
        IEnumerator SpawnWaves()
        {
            m_WaveIndex = 0;

            while (m_WaveIndex < m_NumberOfWaves)
            {
                yield return SpawnWave();

                yield return new WaitForSeconds(m_TimeBetweenWaves);
            }

            yield return new WaitForSeconds(m_RestartDelay);

            m_WaveSpawning = null;
        }

        /// <summary>
        /// Coroutine that spawns a wave of prefab clones, with some time between spawns.
        /// </summary>
        /// <returns></returns>
        IEnumerator SpawnWave()
        {
            for (int i = 0; i < m_SpawnsPerWave; i++)
            {
                SpawnPrefab();

                yield return new WaitForSeconds(m_TimeBetweenSpawns);
            }

            m_WaveIndex++;
        }

        /// <summary>
        /// Spawn a NetworkObject prefab clone.
        /// </summary>
        void SpawnPrefab()
        {
            if (m_NetworkedPrefab == null)
            {
                throw new System.ArgumentNullException("m_NetworkedPrefab");
            }

            // spawn clone right in front of spawner
            var spawnPosition = m_Transform.position + m_Transform.forward;
            var clone = Instantiate(m_NetworkedPrefab, spawnPosition, Quaternion.identity);
            if (!clone.IsSpawned)
            {
                clone.Spawn();
            }
        }

        /// <summary>
        /// Determines whether any player is within range & visible through RaycastNonAlloc check.
        /// </summary>
        /// <returns> True if visible and within range, else false. </returns>
        bool IsAnyPlayerNearbyAndVisible()
        {
            var spawnerPosition = m_Transform.position;

            var ray = new Ray();

            // note: this is not cached to allow runtime modifications to m_ProximityDistance
            var squaredProximityDistance = m_ProximityDistance * m_ProximityDistance;

            // iterate through clients and only return true if a player is in range
            // and is not occluded by a blocking collider.
            foreach (KeyValuePair<ulong, NetworkClient> idToClient in NetworkManager.Singleton.ConnectedClients)
            {
                var playerPosition = idToClient.Value.PlayerObject.transform.position;
                var direction = playerPosition - spawnerPosition;

                if (direction.sqrMagnitude > squaredProximityDistance)
                {
                    continue;
                }

                ray.origin = spawnerPosition;
                ray.direction = direction;

                var hit = Physics.RaycastNonAlloc(ray, m_Hit,
                    Mathf.Min(direction.magnitude, m_ProximityDistance), m_BlockingMask);
                if (hit == 0)
                {
                    return true;
                }
            }

            return false;
        }

        void ReviveSpawner()
        {
            m_NetworkHealthState.HitPoints.Value = m_MaxHP.Value;
        }

        // TODO: David will create interface hookup for receiving hits on non-NPC/PC objects (GOMPS-ID TBD)
        void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            if (!IsServer)
            {
                return;
            }

            m_NetworkHealthState.HitPoints.Value += HP;

            if (m_NetworkHealthState.HitPoints.Value <= 0)
            {
                StopWaveSpawning();
            }
        }
    }
}
