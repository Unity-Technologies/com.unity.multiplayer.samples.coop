using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.Actions;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameState
{
    [RequireComponent(typeof(NetcodeHooks))]
    public class ServerPostGameState : GameStateBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        [FormerlySerializedAs("synchronizedStateData")]
        [SerializeField]
        NetworkPostGame networkPostGame;
        public NetworkPostGame NetworkPostGame => networkPostGame;

        public override GameState ActiveState { get { return GameState.PostGame; } }

        [Inject]
        ConnectionManager m_ConnectionManager;

        [Inject]
        PersistentGameState m_PersistentGameState;

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
                SessionManager<SessionPlayerData>.Instance.OnSessionEnded();
                networkPostGame.WinState.Value = m_PersistentGameState.WinState;
            }
        }

        protected override void OnDestroy()
        {
            //clear actions pool
            ActionFactory.PurgePooledActions();
            m_PersistentGameState.Reset();

            base.OnDestroy();

            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
        }

        public void PlayAgain()
        {
            SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);
        }

        public void GoToMainMenu()
        {
            m_ConnectionManager.RequestShutdown();
        }
    }
}
