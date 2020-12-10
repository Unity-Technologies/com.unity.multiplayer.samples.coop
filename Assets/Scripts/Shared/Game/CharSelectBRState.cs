using System;
using System.Collections.Generic;

using BossRoom;


namespace BossRoom
{
    class CharSelectBRState : IBossRoomState
    {
        protected BossRoomStateManager m_manager;

        public BossRoomState State { get { return BossRoomState.CHARSELECT; } }

        public virtual void Destroy()
        {
        }

        public virtual void Initialize(BossRoomStateManager manager, Dictionary<string, object> stateParams)
        {
            m_manager = manager;
        }

        public virtual void Update()
        {
            //FIXME (temp): as we don't have a CharacterSelect scene yet, we just advance directly to the Game state. 
            m_manager.ChangeState(BossRoomState.GAME, null);
        }
    }
}


