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

        private LobbyResults m_LobbyResults;

        //see note in OnClientConnected
        private const float k_TempSpawnDelaySeconds = 5;

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
                // retrieve the lobby state info so that the players we're about to spawn can query it
                var o = GameStateRelay.GetRelayObject();
                if (o != null && o.GetType() != typeof(LobbyResults))
                    throw new System.Exception("No LobbyResults found!");
                m_LobbyResults = (LobbyResults)o;

                // listen for the client-connect event. This will only happen after
                // the ServerGNHLogic's approval-callback is done, meaning that if we get this event,
                // the client is officially allowed to be here. (And they are joining the game post-lobby...
                // should we do something special here?)
                NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnected;

                // if any other players are already connected to us (i.e. they connected while we were 
                // Now create player characters for all the players
                foreach (var connection in NetworkingManager.Singleton.ConnectedClientsList)
                {
                    //see note in OnClientConnected for why this is a coroutine. 
                    StartCoroutine(CoroSpawnPlayer(connection.ClientId));
                }
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // FIXME: this is a work-around for an MLAPI timing problem which happens semi-reliably;
            // when it happens, it generates the same errors and has the same behavior as this:
            //      https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/328
            // We can't use the workaround suggested there, which is to avoid using MLAPI's scene manager.
            // Instead, we wait a bit for MLAPI to get its state organized, because we can't safely create entities in OnClientConnected().
            // (Note: on further explortation, I think this is due to some sort of scene-loading synchronization: the new client is briefly
            // "in" the lobby screen, but has already told the server it's in the game scene. Or something similar.)
            StartCoroutine(CoroSpawnPlayer(clientId));
        }

        private IEnumerator CoroSpawnPlayer(ulong clientId)
        {
            yield return new WaitForSeconds(k_TempSpawnDelaySeconds);
            SpawnPlayer(clientId);
        }

        private void SpawnPlayer(ulong clientId)
        {
            var newPlayer = Instantiate(m_PlayerPrefab);
            var netState = newPlayer.GetComponent<NetworkCharacterState>();

            var lobbyResults = GetLobbyResultsForClient(clientId);

            netState.CharacterType = lobbyResults.Class;
            netState.CharacterAppearance.Value = lobbyResults.Appearance;
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
