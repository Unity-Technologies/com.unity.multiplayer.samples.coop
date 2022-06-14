using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class ServerPostGameState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.PostGame; } }

        protected override void Awake()
        {
            base.Awake();
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllClientsFinishedLoading;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllClientsFinishedLoading;
            }
        }
        
        static void OnAllClientsFinishedLoading(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                SessionManager<SessionPlayerData>.Instance.OnSessionEnded();
            }
        }
    }
}
