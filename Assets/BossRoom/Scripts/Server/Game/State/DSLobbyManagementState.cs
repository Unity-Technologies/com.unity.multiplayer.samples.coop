using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class DSLobbyManagementState : GameStateBehaviour
    {
        GameNetPortal m_GameNetPortal;
        public override GameState ActiveState => GameState.LobbyManagement;

        [Inject]
        void InjectDependencies(GameNetPortal gameNetPortal)
        {
            m_GameNetPortal = gameNetPortal;
        }

        public void Start()
        {
            // todo start new lobbies and manage finishing games that return here
            m_GameNetPortal.StartIPServer("0.0.0.0", 9998, isHost: false);
        }
    }
}