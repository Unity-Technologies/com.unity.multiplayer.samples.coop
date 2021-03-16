using UnityEngine;

namespace BossRoom.Client
{

    /// <summary>
    /// Client specialization of core BossRoom game logic. 
    /// </summary>
    public class ClientBossRoomState : GameStateBehaviour
    {
        public override GameState ActiveState {  get { return GameState.BossRoom; } }


        public override void NetworkStart()
        {
            base.NetworkStart();
            if( !IsClient ) { this.enabled = false; }
        }

    }

}
