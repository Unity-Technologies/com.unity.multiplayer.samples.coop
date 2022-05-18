using System;
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
            // TODO create DGS lobby here, register to matchmaking, etc. This state bypasses the main menu setup users would normally do get in a game
            // and does its own game setup
            var address = "0.0.0.0";
            var port = 9998;
            DedicatedServerUtilities.Log($"Starting Headless Server, listening on address {address}:{port}");
            m_GameNetPortal.StartIPServer(address, port, isHost: false);
        }
    }
}