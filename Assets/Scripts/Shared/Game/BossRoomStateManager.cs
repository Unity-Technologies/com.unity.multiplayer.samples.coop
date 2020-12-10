using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    public enum BossRoomState
    {
        NONE,
        MAINMENU,
        CHARSELECT,
        GAME
    }

    /// <summary>
    /// The BossRoomStateManager manages the top-level logic for each gamestate
    /// </summary>
    public class BossRoomStateManager : MonoBehaviour
    {
        public GameNetHub NetHub { get; private set; }

        /// <summary>
        /// Gets the active state that BossRoomStateManager is in. 
        /// </summary>
        public BossRoomState ActiveState
        {
            get
            {
                //There is always a client state, and client and server states must always match. See ChangeState. 
                //Note: if converting to a dedicated server, you would need to change this to an #IF BUILD_SERVER #ELSE block
                //that checked either client or server states. See note in ChangeState about converting to a dedicated server. 
                return m_clientState != null ? m_clientState.State : BossRoomState.NONE;
            }
        }

        private IBossRoomState m_serverState;
        private IBossRoomState m_clientState;

        /// <summary>
        /// Transitions from one active state to another. Will no-op if transitioning to the current active state. 
        /// </summary>
        /// <remarks>
        /// On the Host, Server and Client BossRoomState objects run concurrently. On clients, only the "client" version of each
        /// state is instantiated, and for a hypothetical dedicated server, only the server object would be instantiated. If you were
        /// moving to a dedicated server model and stripping out server code from the client, this is one method you would need to change
        /// (for example by making a client and server factory components that plug themselves into the BossRoomStateManager, and are 
        /// guarded by #IF !BUILD_SERVER or #IF BUILD_SERVER, respectively)
        /// </remarks>
        /// <param name="target">the target state to transition to</param>
        /// <param name="stateParams">Any special parameters to inform the next state about, or null for none.</param>
        public void ChangeState(BossRoomState target, Dictionary<string,System.Object> stateParams )
        {
            if( target == ActiveState ) { return; }

            Debug.Log("transitioning from state " + ActiveState + " to " + target);

            switch(target)
            {
                case BossRoomState.NONE:
                    {
                        DestroyStates();
                        m_serverState = null;
                        m_clientState = null;
                        break;
                    }

                case BossRoomState.MAINMENU:
                    {
                        DestroyStates();
                        m_clientState = new BossRoomClient.CMainMenuBRState();
                        m_clientState.Initialize(this, stateParams);
                        break;
                    }

                case BossRoomState.CHARSELECT:
                    {
                        DestroyStates();

                        if( NetHub.NetManager.IsServer )
                        {
                            m_serverState = new BossRoomServer.SCharSelectBRState();
                            m_serverState.Initialize(this, stateParams);
                        }
                        if( NetHub.NetManager.IsClient )
                        {
                            m_clientState = new BossRoomClient.CCharSelectBRState();
                            m_clientState.Initialize(this, stateParams);
                        }

                        break;
                    }

                case BossRoomState.GAME:
                    {
                        DestroyStates();

                        if( NetHub.NetManager.IsServer )
                        {
                            m_serverState = new BossRoomServer.SGameBRState();
                            m_serverState.Initialize(this, stateParams);
                        }
                        if( NetHub.NetManager.IsClient )
                        {
                            m_clientState = new BossRoomClient.CGameBRState();
                            m_clientState.Initialize(this, stateParams);
                        }

                        break;
                    }

                default:
                    throw new System.Exception("unimplemented gamestate detected: " + target);
            }

        }

        /// <summary>
        /// Helper method for ChangeState that destroys the active states if they exist. 
        /// </summary>
        private void DestroyStates()
        {
            if (m_serverState != null) { m_serverState.Destroy(); }
            if (m_clientState != null) { m_clientState.Destroy(); }
            m_serverState = null;
            m_clientState = null;
        }

        // Start is called before the first frame update
        void Start()
        {
            NetHub = this.GetComponent<GameNetHub>();
            ChangeState(BossRoomState.MAINMENU, null);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if( m_serverState != null ) { m_serverState.Update();  }
            if( m_clientState != null ) { m_clientState.Update();  }
        }
    }
}

