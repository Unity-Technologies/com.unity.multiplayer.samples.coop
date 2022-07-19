using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    [RequireComponent(typeof(NetcodeHooks))]
    public class PostGameState : GameStateBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

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
                SessionManager<SessionPlayerData>.Instance.OnSessionEnded();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
        }
    }
}
