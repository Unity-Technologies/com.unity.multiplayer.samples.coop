using System.Collections;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    [RequireComponent(typeof(NetcodeHooks))]
    public class ServerPostGameState : GameStateBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        public const int SecondsToWaitForNewGame = 5;

        public override GameState ActiveState { get { return GameState.PostGame; } }

        protected override void Awake()
        {
            base.Awake();

            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                if (DedicatedServerUtilities.IsServerBuildTarget)
                {
                    IEnumerator WaitAndStartNewGame()
                    {
                        DedicatedServerUtilities.Log($"Waiting a {SecondsToWaitForNewGame} seconds until new game");
                        yield return new WaitForSeconds(SecondsToWaitForNewGame);
                        SceneLoaderWrapper.Instance.LoadScene(SceneNames.CharSelect, useNetworkSceneManager: true);
                    }

                    StartCoroutine(WaitAndStartNewGame());
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
        }
    }
}
