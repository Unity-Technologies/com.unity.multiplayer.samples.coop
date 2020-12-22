using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoom.Client
{

    /// <summary>
    /// Client specialization of core BossRoom game logic. 
    /// </summary>
    public class ClientBossRoomState : GameStateBehaviour
    {
        //!!STUB!! this class will be populated with client-side logic for the main boss-room gameplay experience.

        public override GameState ActiveState {  get { return GameState.BOSSROOM; } }


        public override void NetworkStart()
        {
            base.NetworkStart();
            if( !IsClient ) { this.enabled = false; }
        }
    }

}
