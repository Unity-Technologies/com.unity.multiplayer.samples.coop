using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Component responsible for spawning prefab clones in waves on the server.
    /// <see cref="ServerEnemyPortal"/> calls our SetSpawnerEnabled() to turn on/off spawning.
    /// </summary>
    public class ServerWaveSpawner : NetworkBehaviour
    {
        // networked object that will be spawned in waves
        [SerializeField]
        NetworkObject m_NetworkedPrefab;

        [SerializeField]
        [Tooltip("Each spawned enemy appears at one of the points in this list")]
        List<Transform> m_SpawnPositions;

        [Tooltip("Select which layers will block visibility.")]
        [SerializeField]
        LayerMask m_BlockingMask;

        [Tooltip("Time between player distance & visibility scans, in seconds.")]
        [SerializeField]
        float m_PlayerProximityValidationTimestep = 2;

        [SerializeField]
        [Tooltip("The detection range of spawned entities. Only meaningful for NPCs (not breakables). -1 = \"use default for this NPC\"")]
        float m_SpawnedEntityDetectDistance = -1;

        [Header("Wave parameters")]
        [Tooltip("Total number of waves.")]
        [SerializeField]
        int m_NumberOfWaves = 2;
        [Tooltip("Number of spawns per wave.")]
        [SerializeField]
        int m_SpawnsPerWave = 2;
        [Tooltip("Time between individual spawns, in seconds.")]
        [SerializeField]
        float m_TimeBetweenSpawns = 0.5f;
        [Tooltip("Time between waves, in seconds.")]
        [SerializeField]
        float m_TimeBetweenWaves = 5;
        [Tooltip("Once last wave is spawned, the spawner waits this long to restart wave spawns, in seconds.")]
        [SerializeField]
        float m_RestartDelay = 10;
        [Tooltip("A player must be within this distance to commence first wave spawn.")]
        [SerializeField]
        float m_ProximityDistance = 30;
        [SerializeField]
        [Tooltip("When looking for players within proximity distance, should we count players in stealth mode?")]
        bool m_DetectStealthyPlayers = true;

        [Header("Spawn Cap (i.e. number of simultaneously spawned entities)")]
        [SerializeField]
        [Tooltip("The minimum number of entities this spawner will try to maintain (regardless of player count)")]
        int m_MinSpawnCap = 2;
        [SerializeField]
        [Tooltip("The maximum number of entities this spawner will try to maintain (regardless of player count)")]
        int m_MaxSpawnCap = 10;
        [SerializeField]
        [Tooltip("For each player in the game, the Spawn Cap is raised above the minimum by this amount. (Rounds up to nearest whole number.)")]
        float m_SpawnCapIncreasePerPlayer = 1;

        // cache reference to our own transform
        Transform m_Transform;

        // track wave index and reset once all waves are complete
        int m_WaveIndex;

        // keep reference to our current watch-for-players coroutine
        Coroutine m_WatchForPlayers;

        // keep reference to our wave spawning coroutine
        Coroutine m_WaveSpawning;

        // cache array of RaycastHit as it will be reused for player visibility
        RaycastHit[] m_Hit;

        // indicates whether OnNetworkSpawn() has been called on us yet
        bool m_IsStarted;

        // are we currently spawning stuff?
        bool m_IsSpawnerEnabled;

        // a running tally of spawned entities, used in determining which spawn-point to use next
        int m_SpawnedCount;

        // the currently-spawned entities. We only bother to track these if m_MaxActiveSpawns is non-zero
        List<NetworkObject> m_ActiveSpawns = new List<NetworkObject>();

        void Awake()
        {
            m_Transform = transform;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            m_Hit = new RaycastHit[1];
            m_IsStarted = true;
            if (m_IsSpawnerEnabled)
            {
                StartWaveSpawning();
            }
        }

        public void SetSpawnerEnabled(bool isEnabledNow)
        {
            if (m_IsStarted && m_IsSpawnerEnabled != isEnabledNow)
            {
                if (!isEnabledNow)
                {
                    StopWaveSpawning();
                }
                else
                {
                    StartWaveSpawning();
                }
            }
            m_IsSpawnerEnabled = isEnabledNow;
        }

        void StartWaveSpawning()
        {
            StopWaveSpawning();
            m_WatchForPlayers = StartCoroutine(TriggerSpawnWhenPlayersNear());
        }

        void StopWaveSpawning()
        {
            if (m_WaveSpawning != null)
            {
                StopCoroutine(m_WaveSpawning);
            }
            m_WaveSpawning = null;
            if (m_WatchForPlayers != null)
            {
                StopCoroutine(m_WatchForPlayers);
            }
            m_WatchForPlayers = null;
        }

        public override void OnNetworkDespawn()
        {
            StopWaveSpawning();
        }

        /// <summary>
        /// Coroutine for continually validating proximity to players and starting a wave of enemies in response.
        /// </summary>
        IEnumerator TriggerSpawnWhenPlayersNear()
        {
            while (true)
            {
                if (m_WaveSpawning == null && IsAnyPlayerNearbyAndVisible())
                {
                    m_WaveSpawning = StartCoroutine(SpawnWaves());
                }

                yield return new WaitForSeconds(m_PlayerProximityValidationTimestep);
            }
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
                if (IsRoomAvailableForAnotherSpawn())
                {
                    var newSpawn = SpawnPrefab();
                    m_ActiveSpawns.Add(newSpawn);
                }

                yield return new WaitForSeconds(m_TimeBetweenSpawns);
            }

            m_WaveIndex++;
        }

        /// <summary>
        /// Spawn a NetworkObject prefab clone.
        /// </summary>
        NetworkObject SpawnPrefab()
        {
            if (m_NetworkedPrefab == null)
            {
                throw new System.ArgumentNullException("m_NetworkedPrefab");
            }

            int posIdx = m_SpawnedCount++ % m_SpawnPositions.Count;
            var clone = Instantiate(m_NetworkedPrefab, m_SpawnPositions[posIdx].position, m_SpawnPositions[posIdx].rotation);
            if (!clone.IsSpawned)
            {
                clone.Spawn(true);
            }
            if (m_SpawnedEntityDetectDistance > -1)
            {
                // need to override the spawned creature's detection range (if they even have a detection range!)
                var serverChar = clone.GetComponent<ServerCharacter>();
                if (serverChar && serverChar.AIBrain != null)
                {
                    serverChar.AIBrain.DetectRange = m_SpawnedEntityDetectDistance;
                }
            }

            return clone;
        }

        bool IsRoomAvailableForAnotherSpawn()
        {
            // references to spawned components that no longer exist will become null,
            // so clear those out. Then we know how many we have left
            m_ActiveSpawns.RemoveAll(spawnedNetworkObject => { return spawnedNetworkObject == null; });
            return m_ActiveSpawns.Count < GetCurrentSpawnCap();
        }

        /// <summary>
        /// Returns the current max number of entities we should try to maintain.
        /// This can change based on the current number of living players; if the cap goes below
        /// our current number of active spawns, we don't spawn anything new until we're below the cap.
        /// </summary>
        int GetCurrentSpawnCap()
        {
            int numPlayers = 0;
            foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (serverCharacter.NetState.NetworkLifeState.LifeState.Value == LifeState.Alive)
                {
                    ++numPlayers;
                }
            }

            return Mathf.CeilToInt(Mathf.Min(m_MinSpawnCap + (numPlayers * m_SpawnCapIncreasePerPlayer), m_MaxSpawnCap));
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
            foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                if (!m_DetectStealthyPlayers && serverCharacter.NetState.IsStealthy.Value)
                {
                    // we don't detect stealthy players
                    continue;
                }

                var playerPosition = serverCharacter.physicsWrapper.Transform.position;
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
    }
}
