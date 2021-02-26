using MLAPI;
using System.Collections;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of core BossRoom game logic.
    /// </summary>
    public class ServerBossRoomState : GameStateBehaviour
    {
        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkingManager's list of prefabs!")]
        private NetworkedObject m_PlayerPrefab;

        // note: this is temporary, for testing!
        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkingManager's list of prefabs!")]
        private NetworkedObject m_EnemyPrefab;

        // note: this is temporary, for testing!
        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkingManager's list of prefabs!")]
        private NetworkedObject m_BossPrefab;

        // note: this is temporary, for testing!
        public override GameState ActiveState { get { return GameState.BossRoom; } }

        /// <summary>
        /// This event is raised when all the initial players have entered the game. It is the right time for
        /// other systems to do things like spawn monsters. 
        /// </summary>
        public event System.Action InitialSpawnEvent;

        private LobbyResults m_LobbyResults;

        private GameNetPortal m_NetPortal;
        private ServerGameNetPortal m_ServerNetPortal;

        /// <summary>
        /// Has the ServerBossRoomState already hit its initial spawn? (i.e. spawned players following load from character select). 
        /// </summary>
        public bool InitialSpawnDone { get; private set; }

        public LobbyResults.CharSelectChoice GetLobbyResultsForClient(ulong clientId)
        {
            LobbyResults.CharSelectChoice returnValue;
            if (!m_LobbyResults.Choices.TryGetValue(clientId, out returnValue))
            {
                // We don't know about this client ID! That probably means they joined the game late, after the lobby was closed.
                // We don't yet handle this scenario well (e.g. showing them a "wait for next game" screen, maybe?),
                // so for now we just let them join. We'll pretend that they made them some generic character choices.
                returnValue = new LobbyResults.CharSelectChoice(-1, CharacterTypeEnum.Tank, 0);
                m_LobbyResults.Choices[clientId] = returnValue;
            }
            return returnValue;
        }

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

                // retrieve the lobby state info so that the players we're about to spawn can query it
                var o = GameStateRelay.GetRelayObject();
                if (o != null && o.GetType() != typeof(LobbyResults))
                    throw new System.Exception("No LobbyResults found!");
                m_LobbyResults = (LobbyResults)o;

                DoInitialSpawnIfPossible();
            }
        }

        private bool DoInitialSpawnIfPossible()
        {
            if (m_ServerNetPortal.AreAllClientsInServerScene() && !InitialSpawnDone && m_NetPortal.NetManager.ConnectedClientsList.Count == m_LobbyResults.Choices.Count)
            {
                InitialSpawnDone = true;
                foreach (var kvp in m_LobbyResults.Choices)
                {
                    SpawnPlayer(kvp.Key);
                }
                InitialSpawnEvent?.Invoke();
                return true;
            }
            return false;
        }


        private void OnClientSceneChanged(ulong clientId, int sceneIndex)
        {
            int serverScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            if( sceneIndex == serverScene )
            {
                Debug.Log($"client={clientId} now in scene {sceneIndex}, server_scene={serverScene}, all players in server scene={m_ServerNetPortal.AreAllClientsInServerScene()}");

                bool didSpawn = DoInitialSpawnIfPossible();

                if( !didSpawn && InitialSpawnDone && MLAPI.Spawning.SpawnManager.GetPlayerObject(clientId) == null)
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
            m_NetPortal.ClientSceneChanged -= OnClientSceneChanged;
        }


        private void SpawnPlayer(ulong clientId)
        {
            var newPlayer = Instantiate(m_PlayerPrefab);
            var netState = newPlayer.GetComponent<NetworkCharacterState>();

            var lobbyResults = GetLobbyResultsForClient(clientId);

            var playerData = m_NetPortal.GetComponent<ServerGameNetPortal>().GetPlayerData(clientId);
            string playerName = playerData != null ? playerData.Value.m_PlayerName : ("Player" + lobbyResults.PlayerNumber);

            netState.SetCharacterType(lobbyResults.Class, lobbyResults.Appearance);
            netState.Name = playerName;

            newPlayer.SpawnAsPlayerObject(clientId);
        }

        /// <summary>
        /// Temp code to spawn an enemy
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                var newEnemy = Instantiate(m_EnemyPrefab);
                newEnemy.SpawnWithOwnership(NetworkingManager.Singleton.LocalClientId);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                var newEnemy = Instantiate(m_BossPrefab);
                newEnemy.SpawnWithOwnership(NetworkingManager.Singleton.LocalClientId);
            }
        }
    }
}
