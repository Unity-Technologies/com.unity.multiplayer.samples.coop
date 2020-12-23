using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoom.Client
{
    /// <summary>
    /// Client specialization of the Character Select game state. 
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ClientCharSelectState : GameStateBehaviour
    {
        //!!STUB This class will be populated with logic for the client-side character select class. 

        public override GameState ActiveState { get { return GameState.CHARSELECT; } }


        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsClient) { this.enabled = false; }
        }

    }
}
