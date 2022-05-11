using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerPostGameState : GameStateBehaviour
    {
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

        static void OnAllClientsFinishedLoading(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete && sceneEvent.ClientId != NetworkManager.ServerClientId) return;
            if (NetworkManager.Singleton.IsServer)
            {
                SessionManager<SessionPlayerData>.Instance.OnSessionEnded();
            }
        }
    }
}
