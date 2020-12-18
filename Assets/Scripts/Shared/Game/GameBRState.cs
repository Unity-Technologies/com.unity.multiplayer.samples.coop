using System;
using System.Collections.Generic;

using UnityEngine;

using BossRoom;

namespace BossRoom
{

    /// <summary>
    /// The BossRoomState that runs during primary gameplay.
    /// </summary>
    class GameBRState : IBossRoomState
    {
        protected BossRoomStateManager m_manager;

        public BossRoomState State { get { return BossRoomState.GAME; } }

        //!! STUB !! will be filled out with any gamestate logic shared between server and client. 

        public virtual void Destroy()
        {
        }

        public virtual void Initialize(BossRoomStateManager manager, Dictionary<string, object> stateParams)
        {
            m_manager = manager;
        }

        public virtual void Update()
        {
            
        }
    }
}
