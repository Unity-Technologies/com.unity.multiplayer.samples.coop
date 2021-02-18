using MLAPI;
using System.IO;
using System.Collections;
using UnityEngine;
using MLAPI.SceneManagement;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;

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

        private LobbyResults m_LobbyResults;

        //see note in OnClientConnected
        //private const float k_TempSpawnDelaySeconds = 5;

        //[NSS] This might be a more global value that you could reference
        private const bool m_DeveloperMode = true;

        private void Awake()
        {
            //[NSS] Hook into the scene transition handler's client loaded scene event.
            SceneTransitionHandler.sceneTransitionHandler.clientLoadedScene += SceneTransitionHandler_ClientLoadedScene;
        }

        /// <summary>
        /// SceneTransitionHandler_ClientLoadedScene
        /// Once the scene is 100% loaded (all assets loaded), the client is notified and player spawned.
        /// </summary>
        /// <param name="clientId"></param>
        private void SceneTransitionHandler_ClientLoadedScene(ulong clientId)
        {
            SpawnPlayer(clientId);
        }

        public LobbyResults.CharSelectChoice GetLobbyResultsForClient(ulong clientId)
        {
            LobbyResults.CharSelectChoice returnValue;
            if(m_LobbyResults == null)
            {
                GetRelayObjectInfo();
            }
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

        void GetRelayObjectInfo()
        {
            if(m_LobbyResults == null)
            {
                var o = GameStateRelay.GetRelayObject();
                if (o != null && o.GetType() != typeof(LobbyResults))
                    throw new System.Exception("No LobbyResults found!");
                m_LobbyResults = (LobbyResults)o;
            }
        }

        public override void NetworkStart()
        {
            base.NetworkStart();

            if (IsServer)
            {
                // retrieve the lobby state info so that the players we're about to spawn can query it
                GetRelayObjectInfo();


                // listen for the client-connect event. This will only happen after
                // the ServerGNHLogic's approval-callback is done, meaning that if we get this event,
                // the client is officially allowed to be here. (And they are joining the game post-lobby...
                // should we do something special here?)
                //NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnected;

                // Now create player characters for all the players
                //foreach (var connection in NetworkingManager.Singleton.ConnectedClientsList)
                //{
                //    //see note in OnClientConnected for why this is a coroutine.
                //    StartCoroutine(CoroSpawnPlayer(connection.ClientId));
                //}
                //StartCoroutine(CoroSpawnPlayer(NetworkingManager.Singleton.ServerClientId));
            }
            else
            {
                enabled = false;  //[NSS] with new scene transition handler approach, we can add this back
            }
        }

        /// <summary>
        /// [NSS] This might be needed for joining players.
        /// Note: There doesn't seem to be a way for players to select their player when they join a game in progress.
        /// Note2: Perhaps the joining player should be placed in the lobby first, allowed to select their character, and then upon selection launch into the game in progress?
        /// Note3: If the above (Note2) is the approach, then it is very likely this will not be needed.
        /// </summary>
        /// <param name="clientId"></param>
        private void OnClientConnected(ulong clientId)
        {
            // FIXME: this is a work-around for an MLAPI timing problem which happens semi-reliably;
            // when it happens, it generates the same errors and has the same behavior as this:
            //      https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/328
            // We can't use the workaround suggested there, which is to avoid using MLAPI's scene manager.
            // Instead, we wait a bit for MLAPI to get its state organized, because we can't safely create entities in OnClientConnected().
            // (Note: on further explortation, I think this is due to some sort of scene-loading synchronization: the new client is briefly
            // "in" the lobby screen, but has already told the server it's in the game scene. Or something similar.)
            //StartCoroutine(CoroSpawnPlayer(clientId));
        }


        /// <summary>
        /// NSS: This is no longer needed and could be removed if you like
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private IEnumerator CoroSpawnPlayer(ulong clientId)
        {
            //yield return new WaitForSeconds(k_TempSpawnDelaySeconds);
            SpawnPlayer(clientId);

            yield return null;
        }

        /// <summary>
        /// SpawnPlayer
        /// Spawns the player
        /// </summary>
        /// <param name="clientId">MLAPI client identification number to be spawned</param>
        private void SpawnPlayer(ulong clientId)
        {
            var newPlayer = Instantiate(m_PlayerPrefab);
            var netState = newPlayer.GetComponent<NetworkCharacterState>();

            var lobbyResults = GetLobbyResultsForClient(clientId);

            netState.CharacterType.Value = lobbyResults.Class;
            netState.CharacterAppearance.Value = lobbyResults.Appearance;
            newPlayer.SpawnAsPlayerObject(clientId);
        }

        /// <summary>
        /// Update
        /// This MonoBehaviour update method is used for level specific developer mode functionality
        /// TODO: ?  Remove for final product or perhaps create a UI that can be enabled and contains all of this functionality?
        /// </summary>
        private void Update()
        {
            if(m_DeveloperMode)
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
}
