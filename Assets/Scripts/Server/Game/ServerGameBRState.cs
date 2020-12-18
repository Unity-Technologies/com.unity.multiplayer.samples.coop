using System;
using System.Collections.Generic;

using BossRoom;

namespace BossRoomServer
{
    class ServerGameBRState : GameBRState
    {
        // !! STUB CLASS !!
        // this will be fleshed out with all server-side logic for the Game BossRoom state. 

        public override void Destroy()
        {
            base.Destroy();
        }

        public override void Initialize(BossRoomStateManager manager, Dictionary<string, object> stateParams )
        {
            base.Initialize(manager, stateParams);

            //NOTE: it's very important to use MLAPI's scene switching logic, and not switch the scene yourself. If you do,
            //MLAPI will get confused internally about which scene is active.
            //MLAPI_WISHLIST: could MLAPI listen to scene change events and then handle the networked switch itself, internally?
            MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("SampleScene");
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
