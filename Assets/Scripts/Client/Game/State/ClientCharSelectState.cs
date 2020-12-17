using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoomClient
{
    /// <summary>
    /// Client specialization of the Characterc Select game state. 
    /// </summary>
    public class ClientCharSelectState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.CHARSELECT; } }


        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsClient) { this.enabled = false; }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
