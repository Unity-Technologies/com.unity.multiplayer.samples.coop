using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;

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

        [SerializeField]
        private ServerBossRoomState BossRoomState;

        [SerializeField]
        private List<NetSpawnPoint> m_AuxiliarySpawns;

        public bool FireOnInitialSpawn = true;

        public bool IsBoss = false;

        // Start is called before the first frame update
        void Start()
        {
            if(FireOnInitialSpawn)
            {
                BossRoomState.InitialSpawnEvent += OnInitialSpawn;

                if (BossRoomState.InitialSpawnDone)
                {
                    OnInitialSpawn();
                }
            }
        }

        void OnDestroy()
        {
            BossRoomState.InitialSpawnEvent -= OnInitialSpawn;
        }

        private void OnInitialSpawn()
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
                    BossRoomState.OnBossSpawned(bossNetState);
                }
            }
        }
    }
}
