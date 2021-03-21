using System.Collections.Generic;
using MLAPI;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossRoom.Server
{
    /// <summary>
    /// Because there are some issues associated with placed networked objects and scene transitions, we instead
    /// place these SpawnPoints. They create their associated dynamic entities only after all players have entered the scene.
    /// This spawns all entities with default (server) ownership, but it would be easy to extend this class if necessary to provide
    /// more flexibility.
    /// </summary>
    public class NetSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        public NetworkObject SpawnedObject;

        [FormerlySerializedAs("BossRoomState")]
        [SerializeField]
        ServerBossRoomState m_ServerBossRoomState;

        [SerializeField]
        List<NetSpawnPoint> m_AuxiliarySpawns;

        public bool FireOnInitialSpawn = true;

        public bool IsBoss;

        void Start()
        {
            if(FireOnInitialSpawn)
            {
                m_ServerBossRoomState.InitialSpawnEvent += OnInitialSpawn;

                if (m_ServerBossRoomState.InitialSpawnDone)
                {
                    OnInitialSpawn();
                }
            }
        }

        void OnDestroy()
        {
            m_ServerBossRoomState.InitialSpawnEvent -= OnInitialSpawn;
        }

        void OnInitialSpawn()
        {
            if( SpawnedObject != null )
            {
                var netObj = Instantiate(SpawnedObject, transform.position, transform.rotation);

                var switchedDoor = netObj.GetComponent<ServerSwitchedDoor>();
                if (switchedDoor != null)
                {
                    for( int i = 0; i < m_AuxiliarySpawns.Count; i++ )
                    {
                        var newSwitch = Instantiate(m_AuxiliarySpawns[i].SpawnedObject, m_AuxiliarySpawns[i].transform.position, m_AuxiliarySpawns[i].transform.rotation);

                        switchedDoor.m_SwitchesThatOpenThisDoor.Add(newSwitch.GetComponent<NetworkFloorSwitchState>());
                        newSwitch.Spawn(null,true);
                    }
                }
                // spawn objects with destroyWithScene = true so they clean up properly
                netObj.Spawn(null, true);

                // Special handling for the Boss, connect with BossRoomState to track win condition
                if (IsBoss)
                {
                    var bossNetState = netObj.GetComponent<NetworkCharacterState>();
                    m_ServerBossRoomState.OnBossSpawned(bossNetState);
                }
            }
        }
    }
}
