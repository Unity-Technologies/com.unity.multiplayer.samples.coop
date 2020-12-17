using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of core BossRoom game logic. 
    /// </summary>
    public class ServerBossRoomState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.BOSSROOM; } }


        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer) { this.enabled = false; }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
