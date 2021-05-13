using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// The ServerPostGameState contains logic for displaying UI in either a loss or win state
    /// </summary>
    [RequireComponent(typeof(NetworkWinState))]
    public class ServerPostGameState : GameStateBehaviour
    {
        [SerializeField]
        NetworkWinState m_NetworkWinState;

        [SerializeField]
        BossRoomPlayerRuntimeCollection m_BossRoomPlayers;

        public override GameState ActiveState => GameState.PostGame;

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                if (m_BossRoomPlayers.TryGetPlayer(NetworkManager.LocalClientId,
                    out BossRoomPlayer bossRoomPlayer) &&
                    bossRoomPlayer.TryGetNetworkBehaviour(out NetworkWinState networkWinState))
                {
                    m_NetworkWinState.NetworkWin = networkWinState.NetworkWin;
                }
            }
        }
    }
}
