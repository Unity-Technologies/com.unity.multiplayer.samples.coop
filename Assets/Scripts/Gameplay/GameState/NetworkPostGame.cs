using System;
using Unity.BossRoom.Gameplay.UI;
using Unity.Netcode;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameState
{
    public class NetworkPostGame : NetworkBehaviour
    {
        public NetworkVariable<WinState> WinState = new NetworkVariable<WinState>();

        [Inject]
        PostGameUI m_PostGameUI;
        
        [Inject]
        PersistentGameState m_PersistentGameState;

        public override void OnNetworkSpawn()
        {
            // only hosts can restart the game, other players see a wait message
            m_PostGameUI.Initialize(IsHost);
            
            if (IsServer)
            {
                WinState.Value = m_PersistentGameState.WinState;
            }
        }
    }
}
