using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

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
        [Tooltip("Make sure this is included in the NetworkManager's list of prefabs!")]
        private NetworkObject m_PlayerPrefab;

        // TODO: Remove this as a PC's Character GUID will come from CharSelect scene (GOMPS-550)
        [SerializeField]
        [Tooltip("Make sure these are included in the NetworkManager's list of prefabs!")]
        Avatar[] m_Avatars;

        [SerializeField]
        [Tooltip("A collection of locations for spawning players")]
        private Transform[] m_PlayerSpawnPoints;

        private List<Transform> m_PlayerSpawnPointsList = null;

        public override GameState ActiveState { get { return GameState.BossRoom; } }

        private LobbyResults m_LobbyResults;

        private GameNetPortal m_NetPortal;
        private ServerGameNetPortal m_ServerNetPortal;

        // Wait time constants for switching to post game after the game is won or lost
        private const float k_WinDelay = 7.0f;
        private const float k_LoseDelay = 2.5f;

        /// <summary>
        /// Has the ServerBossRoomState already hit its initial spawn? (i.e. spawned players following load from character select).
        /// </summary>
        public bool InitialSpawnDone { get; private set; }

        //these Ids are recorded for event unregistration at destruction time and are not maintained (the objects they point to may be destroyed during
        //the lifetime of the ServerBossRoomState).
        private List<ulong> m_HeroIds = new List<ulong>();

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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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
                return true;
            }
            return false;
        }


        private void OnClientSceneChanged(ulong clientId, int sceneIndex)
        {
            int serverScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            if (sceneIndex == serverScene)
            {
                Debug.Log($"client={clientId} now in scene {sceneIndex}, server_scene={serverScene}, all players in server scene={m_ServerNetPortal.AreAllClientsInServerScene()}");

                bool didSpawn = DoInitialSpawnIfPossible();

                if (!didSpawn && InitialSpawnDone &&
                    !PlayerServerCharacter.GetPlayerServerCharacters().Find(
                        player => player.OwnerClientId == clientId))
                {
                    //somebody joined after the initial spawn. This is a Late Join scenario. This player may have issues
                    //(either because multiple people are late-joining at once, or because some dynamic entities are
                    //getting spawned while joining. But that's not something we can fully address by changes in
                    //ServerBossRoomState.
                    SpawnPlayer(clientId);
                }

            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (ulong id in m_HeroIds)
            {
                var heroLife = GetLifeStateEvent(id);
                if (heroLife != null)
                {
                    heroLife -= OnHeroLifeStateChanged;
                }
            }

            if (m_NetPortal != null)
            {
                m_NetPortal.ClientSceneChanged -= OnClientSceneChanged;
            }
        }

        /// <summary>
        /// Helper method for OnDestroy that gets the NetworkLifeState.OnValueChanged event for a NetworkObjectId, or null if it doesn't exist.
        /// </summary>
        private NetworkVariable<LifeState>.OnValueChangedDelegate GetLifeStateEvent(ulong id)
        {
            //this is all a little paranoid, because during shutdown it's not always obvious what state is still valid.
            if (NetworkManager != null && NetworkManager.SpawnManager != null && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject netObj) && netObj != null)
            {
                var netState = netObj.GetComponent<NetworkCharacterState>();
                return netState != null ? netState.NetworkLifeState.LifeState.OnValueChanged : null;
            }
            return null;
        }

        private void SpawnPlayer(ulong clientId)
        {
            Transform spawnPoint = null;

            if (m_PlayerSpawnPointsList == null || m_PlayerSpawnPointsList.Count == 0)
            {
                m_PlayerSpawnPointsList = new List<Transform>(m_PlayerSpawnPoints);
            }

            Debug.Assert(m_PlayerSpawnPointsList.Count > 0,
                $"PlayerSpawnPoints array should have at least 1 spawn points.");

            int index = Random.Range(0, m_PlayerSpawnPointsList.Count);
            spawnPoint = m_PlayerSpawnPointsList[index];
            m_PlayerSpawnPointsList.RemoveAt(index);

            Assert.IsTrue(m_RuntimeNetworkObjectsParent && m_RuntimeNetworkObjectsParent.Value,
                "RuntimeNetworkObjectsParent transform is not set!");

            var lobbyResults = GetLobbyResultsForClient(clientId);

            NetworkObject newPlayer = spawnPoint != null ?
                Instantiate(m_PlayerPrefab, spawnPoint.position, spawnPoint.rotation, m_RuntimeNetworkObjectsParent.Value) :
                Instantiate(m_PlayerPrefab, m_RuntimeNetworkObjectsParent.Value);

            if (newPlayer.TryGetComponent(out NetworkAvatarGuidState networkCharacterDefinition))
            {
                networkCharacterDefinition.AvatarGuidArray.Value =
                    m_Avatars[lobbyResults.Appearance].Guid.ToByteArray();
            }

            var netState = newPlayer.GetComponent<NetworkCharacterState>();
            netState.NetworkLifeState.LifeState.OnValueChanged += OnHeroLifeStateChanged;
            m_HeroIds.Add(netState.NetworkObjectId);

            // TODO: Re-implement names on UI (GOMPS-550)
            /*string playerName = m_ServerNetPortal.GetPlayerName(clientId, lobbyResults.PlayerNumber);

            netState.Name = playerName;*/

            // spawn players characters with destroyWithScene = true
            newPlayer.SpawnWithOwnership(clientId, true);
        }

        // Every time a player's life state changes we check to see if game is over
        private void OnHeroLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            // If this Hero is down, check the rest of the party also
            if (lifeState == LifeState.Fainted)
            {
                // Check the life state of all players in the scene
                foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
                {
                    // if any player is alive just retun
                    if (serverCharacter.NetState && serverCharacter.NetState.LifeState == LifeState.Alive)
                    {
                        return;
                    }
                }

                // If we made it this far, all players are down! switch to post game
                StartCoroutine(CoroGameOver(k_LoseDelay, false));
            }
        }

        /// <summary>
        /// Hooked up to GameListener UI event for the event when the boss is defeated.
        /// </summary>
        public void BossDefeated()
        {
            // Boss is dead - set game won to true
            StartCoroutine(CoroGameOver(k_WinDelay, true));
        }

        private IEnumerator CoroGameOver(float wait, bool gameWon)
        {
            // wait 5 seconds for game animations to finish
            yield return new WaitForSeconds(wait);

            GameStateRelay.SetRelayObject(gameWon);
            NetworkManager.Singleton.SceneManager.SwitchScene("PostGame");
        }
    }
}
