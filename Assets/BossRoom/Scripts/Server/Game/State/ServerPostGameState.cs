using System.Collections;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerPostGameState : GameStateBehaviour
    {
        public const int SecondsToWaitForNewGame = 5;

        public override GameState ActiveState { get { return GameState.PostGame; } }

        protected override void Awake()
        {
            base.Awake();
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnAllClientsFinishedLoading;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnAllClientsFinishedLoading;
        }

        void OnAllClientsFinishedLoading(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted || gameObject.scene.name != sceneEvent.SceneName) return;
            if (NetworkManager.Singleton.IsServer)
            {
                SessionManager<SessionPlayerData>.Instance.OnSessionEnded();

                if (DedicatedServerUtilities.IsServerBuildTarget)
                {
                    IEnumerator WaitAndStartNewGame()
                    {
                        DedicatedServerUtilities.Log($"Waiting a {SecondsToWaitForNewGame} seconds until new game");
                        yield return new WaitForSeconds(SecondsToWaitForNewGame); // TODO there should be a UI timer for a countdown for this
                        SceneLoaderWrapper.Instance.LoadScene(SceneNames.CharSelect, useNetworkSceneManager: true);
                    }

                    StartCoroutine(WaitAndStartNewGame());
                }
            }
        }
    }
}
