using System;
using System.Collections.Generic;

using UnityEngine;

using BossRoom;

namespace BossRoom
{

    class GameBRState : IBossRoomState
    {
        protected BossRoomStateManager m_manager;

        public BossRoomState State { get { return BossRoomState.GAME; } }

        public virtual void Destroy()
        {
        }

        public virtual void Initialize(BossRoomStateManager manager, Dictionary<string, object> stateParams)
        {
            m_manager = manager;

            Debug.Log("Entering GameBRState, advancing to SampleScene");

            //this is an example of the slight weirdnesses of having "server" and "client" logic running in parallel on the host. 
            //This will get invoked twice; once as part of the SGameBRState, once as part of the CGameBRState. It might be tempting
            //to move it to the CGameBRState exclusively, but that's not right--it really should happen on a dedicated server too. 
            //The only alternative I can think of is to add an "if(!IsHost)" check around this logic. There will always be some logic
            //which it is not OK to do twice that will need this kind of special handling, although it's not clear to me yet how much. 
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        public virtual void Update()
        {
            
        }
    }
}
