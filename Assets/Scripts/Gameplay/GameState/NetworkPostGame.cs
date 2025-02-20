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
        
        [Inject]
        public void Construct(PersistentGameState persistentGameState, PostGameUI postGameUI)
        {
            m_PostGameUI = postGameUI;
            m_PersistentGameState = persistentGameState;
        }

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
