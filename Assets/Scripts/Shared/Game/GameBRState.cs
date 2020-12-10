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

        public virtual void Destroy()
        {
        }

        public virtual void Initialize(BossRoomStateManager manager, Dictionary<string, object> stateParams)
        {
            m_manager = manager;

            // FIXME_DMW: we probably need to rely on MLAPI's scene management on the client to change scenes, rather than doing it here. 
            // I haven't yet been able to get SceneManagement to work though--on login, the client just stays at the MainMenu scene, even though
            // the server is in the SampleScene. 
            //
            // problems with doing LoadScene here:
            //   1.Runs twice on the host. This is harmless, but not ideal. 
            //   2.This may happen AFTER the server has sent us all dynamic object spawns in ITS scene (because we can't send the event that
            //     triggers this code until after we have returned ConnectionApproval==true to MLAPI). 

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }

        public virtual void Update()
        {
            
        }
    }
}
