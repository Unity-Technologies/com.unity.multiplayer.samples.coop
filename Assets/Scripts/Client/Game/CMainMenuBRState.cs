using System;
using System.Collections.Generic;

using BossRoom;


namespace BossRoomClient
{
    /// <summary>
    /// The BossRoom state logic that runs during MainMenu. Unlike most states, there is only a client variant of this (you are always
    /// a client when sitting at the Main Menu screen). 
    /// </summary>
    class CMainMenuBRState : IBossRoomState
    {
        private BossRoomStateManager m_manager;

        public BossRoomState State { get { return BossRoomState.MAINMENU; } }

        public void Destroy()
        {
        }

        public void Initialize(BossRoomStateManager manager, Dictionary<string, object> stateParams)
        {
            m_manager = manager;

            UnityEngine.Application.targetFrameRate = 60;
        }

        public void Update()
        {
            
        }
    }
}


