using System.Collections;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerPostGameState : GameStateBehaviour
    {
        [SerializeField, Tooltip("Number of seconds to wait for new dedicated server game")]
        int m_SecondsToWaitForNewGame = 5;

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
                        yield return new WaitForSeconds(m_SecondsToWaitForNewGame);
                        SceneLoaderWrapper.Instance.LoadScene(SceneNames.CharSelect, useNetworkSceneManager: true);
                    }

                    StartCoroutine(WaitAndStartNewGame());
                }
            }
        }
    }
}
