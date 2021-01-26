using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoom.Client
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states. 
    /// </summary>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.MainMenu;  } }

        public override void NetworkStart()
        {
            //note: this code won't ever run, because there is no network connection at the main menu screen.
            //fortunately we know you are a client, because all players are clients when sitting at the main menu screen. 
        }
    }

}
