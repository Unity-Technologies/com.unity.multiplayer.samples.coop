using System.Collections;
using System.Collections.Generic;
using BossRoom.Shared;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;

namespace BossRoom.Server
{
    /// <summary>
    /// Component responsible for spawning prefab clones in waves on the server.
    /// </summary>
    public class ServerWaveSpawner : NetworkedBehaviour
    {
        // amount of hits it takes to break any spawner
        const int k_maxHealth = 3;
        
        // current health of the spawner
        public NetworkedVarInt health;
        
        // proposal: reference a RuntimeList of players in game (list for now)
        [SerializeField]
        private List<NetworkedObject> m_Players;
        
        // networked object that will be spawned in waves
        [SerializeField]
        private NetworkedObject m_NetworkedPrefab;

        // cache reference to our own transform
        private Transform m_Transform;
        
        // track wave index and reset once all waves are complete
        private int m_WaveIndex;
        
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
        public float proximityDistance;
        [Tooltip("After being broken, the spawner waits this long to restart wave spawns, in seconds.")]
        public float dormantCooldown;

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
            }
            
            // hack for now
            m_Players.Add(GameObject.FindObjectOfType<NetworkCharacterState>().GetComponent<NetworkedObject>());

            health.Value = k_maxHealth;
            m_Hit = new RaycastHit[1];
            StartPlayerProximityValidation();
        }
        
        void StartPlayerProximityValidation()
        {
            m_WaveSpawning = null;
            StopAllCoroutines();
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
                SpawnPrefabClientRpc();

                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            m_WaveIndex++;
        }
        
        // NOTE: This is not being fired on the Host (the server-client)
        /// <summary>
        /// Server Rpc to spawn a NetworkedObject prefab clone.
        /// </summary>
        [ClientRpc]
        void SpawnPrefabClientRpc()
        {
            if (m_NetworkedPrefab == null)
            {
                return;
            }

            var spawnPosition = m_Transform.position + 
                                new Vector3(UnityEngine.Random.Range(-1f, 1f), 
                                    UnityEngine.Random.Range(-1f, 1f), 
                                    UnityEngine.Random.Range(-1f, 1f));
            var clone =  Instantiate(m_NetworkedPrefab, spawnPosition, Quaternion.identity);
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

            // iterate through players and only return true if a player is in range
            // and is not blocked by a blocking collider.
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

                // TODO: sort out layer for terrain/walls/blocker that this ray will try to hit
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

            // revive spawner back up to full health and visually show a revival
            ReviveClientRpc();
            health.Value = k_maxHealth;
            StartPlayerProximityValidation();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                return;
            }
            
            // TODO: sort out layer for player weapon that this collider will listen for (through Physics matrix)
            if (!other.gameObject.CompareTag(("Weapon")))
            {
                return;
            }

            health.Value--;

            if (health.Value <= 0)
            {
                BrokenClientRpc();
                
                // this spawner is dead; commence a cooldown coroutine before being revived
                StartWaveSpawnCooldown();
            }
            else
            {
                ReceiveHitClientRpc();
            }
        }

        /// <summary>
        /// RPC sent from the server to display on client side that this spawner has been damaged.
        /// </summary>
        [ClientRpc]
        void ReceiveHitClientRpc()
        {
            // TODO: fire hit animation here
        }
        
        /// <summary>
        /// RPC sent from the server to display on client side that this spawner has been broken.
        /// </summary>
        [ClientRpc]
        void BrokenClientRpc()
        {
            // TODO: fire die animation here
        }
        
        /// <summary>
        /// RPC sent from the server to display on client side that this spawner has been revived.
        /// </summary>
        [ClientRpc]
        void ReviveClientRpc()
        {
            // TODO: fire revive animation here
        }
    }
}