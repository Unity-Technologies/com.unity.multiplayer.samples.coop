using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using VContainer;
using Random = UnityEngine.Random;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// Server specialization of core BossRoom game logic.
    /// </summary>
    public class ServerBossRoomState : GameStateBehaviour
    {
        [SerializeField]
        TransformVariable m_NetworkGameStateTransform;

        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkManager's list of prefabs!")]
        private NetworkObject m_PlayerPrefab;

        [SerializeField]
        [Tooltip("A collection of locations for spawning players")]
        private Transform[] m_PlayerSpawnPoints;

        private List<Transform> m_PlayerSpawnPointsList = null;

        public override GameState ActiveState { get { return GameState.BossRoom; } }

        // Wait time constants for switching to post game after the game is won or lost
        private const float k_WinDelay = 7.0f;
        private const float k_LoseDelay = 2.5f;

        /// <summary>
        /// Has the ServerBossRoomState already hit its initial spawn? (i.e. spawned players following load from character select).
        /// </summary>
        public bool InitialSpawnDone { get; private set; }

        /// <summary>
        /// Keeping the subscriber during this GameState's lifetime to allow disposing of subscription and re-subscribing
        /// when despawning and spawning again.
        /// </summary>
        [Inject] ISubscriber<LifeStateChangedEventMessage> m_LifeStateChangedEventMessageSubscriber;

        IDisposable m_Subscription;

        [Inject] ConnectionManager m_ConnectionManager;

        protected override void Awake()
        {
            base.Awake();
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
            NetworkManager.Singleton.SceneManager.OnUnloadComplete += OnServerUnloadComplete;
            DedicatedServerUtilities.PrintSceneHierarchy();
        }

        void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                ServerLoaded();
            }
        }

        private void ServerLoaded()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                // reset win state
                SetWinState(WinState.Invalid);

                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
                NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;

                SessionManager<SessionPlayerData>.Instance.OnSessionStarted();
                m_Subscription = m_LifeStateChangedEventMessageSubscriber.Subscribe(OnLifeStateChangedEventMessage);
            }
        }

        protected override void OnDestroy()
        {
            m_Subscription?.Dispose();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
                NetworkManager.Singleton.SceneManager.OnUnloadComplete -= OnServerUnloadComplete;
            }

            base.OnDestroy();
        }

        void OnSynchronizeComplete(ulong clientId)
        {
            if (InitialSpawnDone &&
                !PlayerServerCharacter.GetPlayerServerCharacter(clientId))
            {
                //somebody joined after the initial spawn. This is a Late Join scenario. This player may have issues
                //(either because multiple people are late-joining at once, or because some dynamic entities are
                //getting spawned while joining. But that's not something we can fully address by changes in
                //ServerBossRoomState.
                SpawnPlayer(clientId, true);
            }
        }

        void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!InitialSpawnDone && loadSceneMode == LoadSceneMode.Single)
            {
                InitialSpawnDone = true;
                foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
                {
                    SpawnPlayer(kvp.Key, false);
                }
            }
        }

        void OnClientDisconnect(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                // If a client disconnects, check for game over in case all other players are already down
                StartCoroutine(WaitToCheckForGameOver());
            }
        }

        IEnumerator WaitToCheckForGameOver()
        {
            // Wait until next frame so that the client's player character has despawned
            yield return null;
            CheckForGameOver();
        }

        public void OnServerUnloadComplete(ulong clientId, string sceneName)
        {
            if (clientId != NetworkManager.ServerClientId) return;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
            m_Subscription?.Dispose();
        }

        private void SpawnPlayer(ulong clientId, bool lateJoin)
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

            var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            var newPlayer = Instantiate(m_PlayerPrefab, Vector3.zero, Quaternion.identity);

            var newPlayerCharacter = newPlayer.GetComponent<ServerCharacter>();

            var physicsTransform = newPlayerCharacter.physicsWrapper.Transform;

            if (spawnPoint != null)
            {
                physicsTransform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

            var persistentPlayerExists = playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer);
            Assert.IsTrue(persistentPlayerExists,
                $"Matching persistent PersistentPlayer for client {clientId} not found!");

            // pass character type from persistent player to avatar
            var networkAvatarGuidStateExists =
                newPlayer.TryGetComponent(out NetworkAvatarGuidState networkAvatarGuidState);

            Assert.IsTrue(networkAvatarGuidStateExists,
                $"NetworkCharacterGuidState not found on player avatar!");

            // if reconnecting, set the player's position and rotation to its previous state
            if (lateJoin)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
                if (sessionPlayerData is { HasCharacterSpawned: true })
                {
                    physicsTransform.SetPositionAndRotation(sessionPlayerData.Value.PlayerPosition, sessionPlayerData.Value.PlayerRotation);
                }
            }

            networkAvatarGuidState.AvatarGuid.Value =
                persistentPlayer.NetworkAvatarGuidState.AvatarGuid.Value;

            // pass name from persistent player to avatar
            if (newPlayer.TryGetComponent(out NetworkNameState networkNameState))
            {
                networkNameState.Name.Value = persistentPlayer.NetworkNameState.Name.Value;
            }

            // spawn players characters with destroyWithScene = true
            newPlayer.SpawnWithOwnership(clientId, true);
        }

        static IEnumerator WaitToReposition(Transform moveTransform, Vector3 newPosition, Quaternion newRotation)
        {
            yield return new WaitForSeconds(1.5f);
            moveTransform.SetPositionAndRotation(newPosition, newRotation);
        }

        void OnLifeStateChangedEventMessage(LifeStateChangedEventMessage message)
        {
            switch (message.CharacterType)
            {
                case CharacterTypeEnum.Tank:
                case CharacterTypeEnum.Archer:
                case CharacterTypeEnum.Mage:
                case CharacterTypeEnum.Rogue:
                    // Every time a player's life state changes to fainted we check to see if game is over
                    if (message.NewLifeState == LifeState.Fainted)
                    {
                        CheckForGameOver();
                    }

                    break;
                case CharacterTypeEnum.ImpBoss:
                    if (message.NewLifeState == LifeState.Dead)
                    {
                        // Boss is dead - set game won to true
                        StartCoroutine(CoroGameOver(k_WinDelay, true));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void CheckForGameOver()
        {
            // Check the life state of all players in the scene
            foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                // if any player is alive just return
                if (serverCharacter.NetState && serverCharacter.NetState.LifeState == LifeState.Alive)
                {
                    return;
                }
            }

            // If we made it this far, all players are down! switch to post game
            StartCoroutine(CoroGameOver(k_LoseDelay, false));
        }


        void SetWinState(WinState winState)
        {
            if (m_NetworkGameStateTransform && m_NetworkGameStateTransform.Value &&
                m_NetworkGameStateTransform.Value.TryGetComponent(out NetworkGameState networkGameState))
            {
                networkGameState.NetworkWinState.winState.Value = winState;
            }
        }

        private IEnumerator CoroGameOver(float wait, bool gameWon)
        {
            // wait 5 seconds for game animations to finish
            yield return new WaitForSeconds(wait);

            SetWinState(gameWon ? WinState.Win : WinState.Loss);

            SceneLoaderWrapper.Instance.LoadScene(SceneNames.PostGame, useNetworkSceneManager: true);
        }
    }
}
