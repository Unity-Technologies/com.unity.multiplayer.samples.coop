using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkedVar;
using MLAPI.Serialization.Pooled;

namespace BossRoom.Server
{
    /// <summary>
    /// Component responsible for spawning prefab clones in waves on the server.
    /// </summary>
    [RequireComponent(typeof(Collider)), RequireComponent(typeof(Animator))]
    public class ServerWaveSpawner : NetworkedBehaviour
    {
        // amount of hits it takes to break any spawner
        const int k_MaxHealth = 3;
        
        // current health of the spawner
        [SerializeField]
        NetworkedVarInt m_Health;
        
        // cache reference to our animator
        [SerializeField]
        Animator m_Animator;
        
        // proposal: reference a RuntimeList of players in game (list for now)
        List<NetworkedObject> m_Players;
        
        // networked object that will be spawned in waves
        [SerializeField]
        NetworkedObject m_NetworkedPrefab;

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

            m_Players = new List<NetworkedObject>();
            // TODO: replace block below with proper getter for players or RuntimeList (GOMPS-124)
            NetworkCharacterState[] networkCharacterStates = FindObjectsOfType<NetworkCharacterState>();
            foreach (var networkCharacterState in networkCharacterStates)
            {
                m_Players.Add(networkCharacterState.GetComponent<NetworkedObject>());
            }

            m_Health.Value = k_MaxHealth;
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
                if (m_Health.Value <= 0)
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
        /// Spawn a NetworkedObject prefab clone.
        /// </summary>
        void SpawnPrefab()
        {
            if (m_NetworkedPrefab == null)
            {
                throw new System.ArgumentNullException("m_NetworkedPrefab");
            }

            // spawn clone right in front of spawner
            var spawnPosition = m_Transform.position + m_Transform.forward;
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
            if (m_Players == null || m_Players.Count == 0)
            {
                return false;
            }

            var spawnerPosition = m_Transform.position;
            var proximityDistanceSquared = m_ProximityDistance * m_ProximityDistance;

            var ray = new Ray();

            // iterate through players and only return true if a player is in range
            // and is not blocked by a blocking collider.
            foreach (var player in m_Players)
            {
                var playerPosition = player.transform.position;
                var direction = playerPosition - spawnerPosition;
                
                if (Vector3.SqrMagnitude(direction) > proximityDistanceSquared)
                {
                    continue;
                }

                ray.origin = spawnerPosition;
                ray.direction = direction;

                var hit = Physics.RaycastNonAlloc(ray, m_Hit, 
                    Mathf.Min(direction.magnitude, m_ProximityDistance),m_BlockingMask);
                if (hit == 0)
                {
                    return true;
                }
            }
            
            return false;
        }

        void ReviveSpawner()
        {
            m_Health.Value = k_MaxHealth;
            ServerBroadcast(ReviveClientRpc);
        }
        
        // TODO: David will create interface hookup for receiving hits on non-NPC/PC objects (GOMPS-ID TBD)
        void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            if (!IsServer)
            {
                return;
            }

            m_Health.Value += HP;

            if (m_Health.Value <= 0)
            {
                StopWaveSpawning();
                ServerBroadcast(BrokenClientRpc);
            }
            else
            {
                ServerBroadcast(ReceiveHitClientRpc);
            }
        }
        
        /// <summary>
        /// Server->Client RPC that broadcasts a certain RpcDelegate to fire on all clients.
        /// </summary>
        /// <param name="rpcDelegate"> RpcDelegate that will fire on each client. </param>
        void ServerBroadcast(RpcDelegate rpcDelegate)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                InvokeClientRpcOnEveryonePerformance(rpcDelegate, stream);
            }
        }

        /// <summary>
        /// RPC sent from the server to display on client side that this spawner has been damaged.
        /// </summary>
        [ClientRPC]
        void ReceiveHitClientRpc(ulong clientId, Stream stream)
        {
            // TODO: fire hit animation here (GOMPS-123)
        }
        
        /// <summary>
        /// RPC sent from the server to display on client side that this spawner has been broken.
        /// </summary>
        [ClientRPC]
        void BrokenClientRpc(ulong clientId, Stream stream)
        {
            // TODO: fire die animation here (GOMPS-123)
        }
        
        /// <summary>
        /// RPC sent from the server to display on client side that this spawner has been revived.
        /// </summary>
        [ClientRPC]
        void ReviveClientRpc(ulong clientId, Stream stream)
        {
            // TODO: fire revive animation here (GOMPS-123) 
        }
    }
}