using MLAPI;
using MLAPI.Spawning;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of core BossRoom game logic.
    /// </summary>
    public class ServerBossRoomState : GameStateBehaviour
    {
        [SerializeField]
        TransformVariable m_RuntimeNetworkObjectsParent;

        [SerializeField]
        BossRoomPlayerRuntimeCollection m_BossRoomPlayers;

        [SerializeField]
        BossRoomPlayerCharacterRuntimeCollection m_BossRoomPlayerCharacters;

        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkManager's list of prefabs!")]
        NetworkObject m_PlayerPrefab;

        [SerializeField]
        [Tooltip("A collection of locations for spawning players")]
        Transform[] m_PlayerSpawnPoints;

        List<Transform> m_PlayerSpawnPointsList;

        public override GameState ActiveState => GameState.BossRoom;

        GameNetPortal m_NetPortal;
        ServerGameNetPortal m_ServerNetPortal;

        // Wait time constants for switching to post game after the game is won or lost
        const float k_WinDelay = 7.0f;
        const float k_LoseDelay = 2.5f;

        /// <summary>
        /// Has the ServerBossRoomState already hit its initial spawn? (i.e. spawned players following load from character select).
        /// </summary>
        public bool InitialSpawnDone { get; private set; }

        //these Ids are recorded for event unregistration at destruction time and are not maintained (the objects they point to may be destroyed during
        //the lifetime of the ServerBossRoomState).
        List<ulong> m_HeroIds = new List<ulong>();

        public override void NetworkStart()
        {
            base.NetworkStart();

            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                m_NetPortal = GameObject.FindGameObjectWithTag("GameNetPortal").GetComponent<GameNetPortal>();
                m_ServerNetPortal = m_NetPortal.GetComponent<ServerGameNetPortal>();

                m_NetPortal.ClientSceneChanged += OnClientSceneChanged;

                // reset players' win state on game start
                SetPlayersWinState(WinState.Invalid);

                DoInitialSpawnIfPossible();
            }
        }

        bool DoInitialSpawnIfPossible()
        {
            if (m_ServerNetPortal.AreAllClientsInServerScene() &&
                !InitialSpawnDone &&
                NetworkManager.Singleton.ConnectedClientsList.Count == m_BossRoomPlayers.Items.Count)
            {
                InitialSpawnDone = true;
                foreach (var bossRoomPlayerData in m_BossRoomPlayers.Items)
                {
                    SpawnPlayer(bossRoomPlayerData.OwnerClientId);
                }
                return true;
            }
            return false;
        }

        void OnClientSceneChanged(ulong clientId, int sceneIndex)
        {
            int serverScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            if (sceneIndex == serverScene)
            {
                Debug.Log($"client={clientId} now in scene {sceneIndex}, server_scene={serverScene}, all players in server scene={m_ServerNetPortal.AreAllClientsInServerScene()}");

                bool didSpawn = DoInitialSpawnIfPossible();

                if (!didSpawn &&
                    InitialSpawnDone &&
                    !m_BossRoomPlayerCharacters.TryGetPlayerCharacter(clientId, out BossRoomPlayerCharacter bossRoomPlayer))
                {
                    //somebody joined after the initial spawn. This is a Late Join scenario. This player may have issues
                    //(either because multiple people are late-joining at once, or because some dynamic entities are
                    //getting spawned while joining. But that's not something we can fully address by changes in
                    //ServerBossRoomState.
                    SpawnPlayer(clientId);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (ulong id in m_HeroIds)
            {
                RemoveLifeStateListener(id);
            }

            if (m_NetPortal != null)
            {
                m_NetPortal.ClientSceneChanged -= OnClientSceneChanged;
            }
        }

        /// <summary>
        /// Helper method for OnDestroy that removes a subscription from a NetworkObject's NetworkLifeState component.
        /// </summary>
        void RemoveLifeStateListener(ulong id)
        {
            //this is all a little paranoid, because during shutdown it's not always obvious what state is still valid.
            if (NetworkSpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject networkObject) && networkObject)
            {
                var networkLifeState = networkObject.GetComponent<NetworkLifeState>();
                if (networkLifeState)
                {
                    networkLifeState.RemoveListener(OnHeroLifeStateChanged);
                }
            }
        }

        void SpawnPlayer(ulong clientId)
        {
            if (m_PlayerSpawnPointsList == null || m_PlayerSpawnPointsList.Count == 0)
            {
                m_PlayerSpawnPointsList = new List<Transform>(m_PlayerSpawnPoints);
            }

            Debug.Assert(m_PlayerSpawnPointsList.Count > 0,
                $"PlayerSpawnPoints array should have at least 1 spawn points.");

            int index = Random.Range(0, m_PlayerSpawnPointsList.Count);
            Transform spawnPoint = m_PlayerSpawnPointsList[index];
            m_PlayerSpawnPointsList.RemoveAt(index);

            Assert.IsTrue(m_RuntimeNetworkObjectsParent && m_RuntimeNetworkObjectsParent.Value,
                "RuntimeNetworkObjectsParent transform is not set!");

            var newPlayer = spawnPoint != null ?
                Instantiate(m_PlayerPrefab, spawnPoint.position, spawnPoint.rotation, m_RuntimeNetworkObjectsParent.Value) :
                Instantiate(m_PlayerPrefab, m_RuntimeNetworkObjectsParent.Value);

            var networkLifeState = newPlayer.GetComponent<NetworkLifeState>();
            networkLifeState.AddListener(OnHeroLifeStateChanged);
            m_HeroIds.Add(networkLifeState.NetworkObjectId);

            // spawn players characters with destroyWithScene = true
            newPlayer.SpawnWithOwnership(clientId, null, true);
        }

        // Every time a player's life state changes we check to see if game is over
        void OnHeroLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            // If this Hero is down, check the rest of the party also
            if (lifeState == LifeState.Fainted)
            {
                // Check the life state of all players in the scene
                foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
                {
                    // if any player is alive just return
                    if (serverCharacter.NetState &&
                        serverCharacter.NetworkLifeState.NetworkLife == LifeState.Alive)
                    {
                        return;
                    }
                }

                // If we made it this far, all players are down! switch to post game
                StartCoroutine(WaitToEndGame(k_LoseDelay, false));
            }
        }

        /// <summary>
        /// Hooked up to GameListener UI event for the event when the boss is defeated.
        /// </summary>
        public void BossDefeated()
        {
            // Boss is dead - set game won to true
            StartCoroutine(WaitToEndGame(k_WinDelay, true));
        }

        void SetPlayersWinState(WinState winState)
        {
            for (int i = 0; i < m_BossRoomPlayers.Items.Count; i++)
            {
                if (m_BossRoomPlayers.Items[i].TryGetNetworkBehaviour(out NetworkWinState networkWinState))
                {
                    networkWinState.NetworkWin = winState;
                }
            }
        }

        IEnumerator WaitToEndGame(float wait, bool gameWon)
        {
            SetPlayersWinState(gameWon ? WinState.Win : WinState.Loss);

            // wait 5 seconds for game animations to finish
            yield return new WaitForSeconds(wait);

            MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("PostGame");
        }
    }
}
