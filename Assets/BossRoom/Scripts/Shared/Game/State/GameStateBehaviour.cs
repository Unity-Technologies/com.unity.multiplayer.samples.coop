using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

namespace BossRoom
{
    public enum GameState
    {
        MainMenu,
        CharSelect,
        BossRoom,
    }

    /// <summary>
    /// A special kind of NetworkedBehaviour that represents a discrete game state. The special feature it offers is
    /// that it provides some guarantees that only one such GameState will be running at a time. 
    /// </summary>
    /// <remarks>
    /// Q: what is the relationship between a GameState and a Scene?
    /// A: There is a 1-to-many relationship between states and scenes. That is, every scene corresponds to exactly one state, 
    ///    but a single state can exist in multiple scenes. 
    /// Q: How do state transitions happen?
    /// A: They are driven implicitly by calling MLAPI.SceneManagement.NetworkSceneManager.SwitchScene in server code. This is
    ///    important, because if state transitions were driven separately from scene transitions, then states that cared what
    ///    scene they ran in would need to carefully synchronize their logic to scene loads. 
    /// Q: How many GameStateBehaviours are there?
    /// A: Exactly one on the server and one on the client (on the host a server and client GameStateBehaviour will run concurrently, as
    ///    with other networked prefabs). 
    /// Q: If these are MonoBehaviours, how do you have a single state that persists across multiple scenes?
    /// A: Set your Persists property to true. If you transition to another scene that has the same gamestate, the 
    ///    current GameState object will live on, and the version in the new scene will suicide to make room for it. 
    ///    
    /// Important Note: We assume that every Scene has a GameState object. If not, then it's possible that a Persisting game state
    /// will outlast its lifetime (as there is no successor state to clean it up). 
    /// </remarks>
    public abstract class GameStateBehaviour : NetworkedBehaviour
    {
        /// <summary>
        /// Does this GameState persist across multiple scenes?
        /// </summary>
        public virtual bool Persists { get { return false; } }

        /// <summary>
        /// What GameState this represents. Server and client specializations of a state should always return the same enum. 
        /// </summary>
        public abstract GameState ActiveState {get; }

        /// <summary>
        /// This is the single active GameState object. There can be only one. 
        /// </summary>
        private static GameObject s_activeStateGO;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if( s_activeStateGO != null )
            {
                if( s_activeStateGO == this.gameObject )
                {
                    //nothing to do here, if we're already the active state object. 
                    return;
                }

                //on the host, this might return either the client or server version, but it doesn't matter which;
                //we are only curious about its type, and its persist state. 
                var prev_state = s_activeStateGO.GetComponent<GameStateBehaviour>();

                if( prev_state.Persists && prev_state.ActiveState == this.ActiveState )
                {
                    //we need to make way for the DontDestroyOnLoad state that already exists.
                    Object.Destroy(this.gameObject);
                    return;
                }

                //otherwise, the old state is going away. Either it wasn't a Persisting state, or it was, 
                //but we're a different kind of state. In either case, we're going to be replacing it. 
                Object.Destroy(s_activeStateGO);
            }

            s_activeStateGO = this.gameObject;
            if( Persists ) { Object.DontDestroyOnLoad(this.gameObject); }
        }

        protected virtual void OnDestroy()
        {
            if( !Persists )
            {
                s_activeStateGO = null;
            }
        }
    }

}

