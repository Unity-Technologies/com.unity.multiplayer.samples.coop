using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

namespace BossRoom.Server
{
    /// <summary>
    /// Component responsible for spawning prefab clones in waves on the server.
    /// </summary>
    public class ServerWaveSpawner : MonoBehaviour
    {
        // proposal: reference a RuntimeList of players in game (list for now)
        [SerializeField]
        private List<NetworkedObject> m_Players;
        
        // networked object that will be spawned in waves
        [SerializeField]
        private NetworkedObject m_NetworkedPrefab;

        // cache reference to our own transform
        private Transform m_Transform;
        
        // track wave index and reset once all waves are complete
        private int m_WaveIndex = 0;
        
        // keep reference to our wave spawning coroutine
        private Coroutine m_WaveSpawning;
        
        // cache our Ray as it will be reused for player visibility
        private Ray m_Ray;
        
        // cache array of RaycastHit as it will be reused for player visibility
        private RaycastHit[] m_Hit;

        [Tooltip("Select which layers will block visibility.")]
        [SerializeField]
        private LayerMask m_BlockingMask;

        [Tooltip("Time between player distance & visibility scans, in seconds.")]
        public float playerDistanceCheckHeartbeat;
        
        [Header("Wave parameters")]
        [Tooltip("Total number of waves.")]
        public int numberOfWaves;
        [Tooltip("Number of spawns per wave.")]
        public int spawnsPerWave;
        [Tooltip("Time between individual spawns, in seconds.")]
        public float timeBetweenSpawns;
        [Tooltip("Time between waves, in seconds.")]
        public float timeBetweenWaves;
        [Tooltip("Once last wave is spawned, the spawner waits this long to restart wave spawns, in seconds.")]
        public float restartDelay;
        [Tooltip("A player must be withing this distance to commence first wave spawn.")]
        public float proximityDistance = 50f;
        [Tooltip("After being broken, the spawner waits this long to restart wave spawns, in seconds.")]
        public float dormantCooldown = 60f;

        // TODO [GOMPS-81] Current workaround for not inheriting from NetworkedBehaviour for NetworkStart event
        private bool m_ServerStartedCallbackSet;

        void Awake()
        {
            m_Transform = transform;
        }

        // TODO [GOMPS-81] Invoke StartPlayerProximityValidation on proper NetworkStart callback 
        void Update()
        {
            if (NetworkingManager.Singleton == null || m_ServerStartedCallbackSet)
            {
                return;
            }

            NetworkingManager.Singleton.OnServerStarted += StartPlayerProximityValidation;
            m_ServerStartedCallbackSet = true;
            m_Hit = new RaycastHit[1];
        }
        
        void StartPlayerProximityValidation()
        {
            StopAllCoroutines();
            StartCoroutine(ValidatePlayersProximity(StartWaveSpawning));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationAction"></param>
        /// <returns></returns>
        IEnumerator ValidatePlayersProximity(System.Action validationAction)
        {
            while (true)
            {
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
                
                yield return new WaitForSeconds(playerDistanceCheckHeartbeat);
            }
        }

        void StartWaveSpawning()
        {
            if (m_WaveSpawning != null)
            {
                StopCoroutine(m_WaveSpawning);
            }

            m_WaveSpawning = StartCoroutine(WaveSpawn());
        }
        
        // TODO: [ServerOnly] attribute
        /// <summary>
        /// Coroutine for spawning prefabs clones in waves, waiting a duration before spawning a new wave.
        /// Once all waves are completed, it waits a restart time before termination. 
        /// </summary>
        /// <returns></returns>
        IEnumerator WaveSpawn()
        {
            m_WaveIndex = 0;
            
            while (m_WaveIndex < numberOfWaves)
            {
                yield return SpawnWave();
                
                yield return new WaitForSeconds(timeBetweenWaves);
            }
            
            yield return new WaitForSeconds(restartDelay);

            m_WaveSpawning = null;
        }

        // TODO: [ServerOnly] attribute
        /// <summary>
        /// Coroutine that spawns a wave of prefab clones, with some time between spawns.
        /// </summary>
        /// <returns></returns>
        IEnumerator SpawnWave()
        {
            for (var i = 0; i < spawnsPerWave; i++)
            {
                SpawnPrefabServerRpc();

                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            m_WaveIndex++;
        }

        /// <summary>
        /// Server Rpc to spawn a NetworkedObject prefab clone.
        /// </summary>
        [ServerRpc]
        void SpawnPrefabServerRpc()
        {
            if (m_NetworkedPrefab == null)
            {
                return;
            }
            
            var clone =  Instantiate(m_NetworkedPrefab, m_Transform.position, Quaternion.identity);
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
            if (m_Players == null)
            {
                return false;
            }

            var spawnerPosition = m_Transform.position;

            foreach (var player in m_Players)
            {
                var playerPosition = player.transform.position;
                
                if (Vector3.Distance(playerPosition, spawnerPosition) >
                    proximityDistance)
                {
                    continue;
                }

                var direction = playerPosition - spawnerPosition;
                m_Ray = new Ray(spawnerPosition, direction);

                var hit = Physics.RaycastNonAlloc(m_Ray, m_Hit, 
                    Mathf.Min(direction.magnitude, proximityDistance),m_BlockingMask);
                if (hit == 0)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Immediately stops current wave spawns and restarts proximity check after cooldown. 
        /// </summary>
        [ContextMenu("Cooldown")]
        void StartWaveSpawnCooldown()
        {
            StopAllCoroutines();
            StartCoroutine(WaveSpawnCooldown());
        }

        /// <summary>
        /// Coroutine to wait a duration and restart player proximity validation.
        /// </summary>
        /// <returns></returns>
        IEnumerator WaveSpawnCooldown()
        {
            yield return new WaitForSeconds(dormantCooldown);
            
            StartPlayerProximityValidation();
        }
    }
}