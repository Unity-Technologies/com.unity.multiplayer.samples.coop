using System;
using UnityEngine;
using VContainer.Unity;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public enum GameState
    {
        MainMenu,
        LobbyManagement,
        CharSelect,
        BossRoom,
        PostGame
    }

    /// <summary>
    /// A special kind of NetworkBehaviour that represents a discrete game state. The special feature it offers is
    /// that it provides some guarantees that only one such GameState will be running at a time.
    /// </summary>
    /// <remarks>
    /// Q: what is the relationship between a GameState and a Scene?
    /// A: There is a 1-to-many relationship between states and scenes. That is, every scene corresponds to exactly one state,
    ///    but a single state can exist in multiple scenes.
    /// Q: How do state transitions happen?
    /// A: They are driven implicitly by calling NetworkManager.SceneManager.LoadScene in server code. This is
    ///    important, because if state transitions were driven separately from scene transitions, then states that cared what
    ///    scene they ran in would need to carefully synchronize their logic to scene loads.
    /// Q: How many GameStateBehaviours are there?
    /// A: Exactly one on the server and one on the client (on the host a server and client GameStateBehaviour will run concurrently, as
    ///    with other networked prefabs).
    /// Q: If these are MonoBehaviours, how do you have a single state that persists across multiple scenes?
    /// A: Set your Persists property to true. If you transition to another scene that has the same gamestate, the
    ///    current GameState object will live on, and the version in the new scene will self destroy to make room for it.
    ///
    /// Important Note: We assume that every Scene has a GameState object. If not, then it's possible that a Persisting game state
    /// will outlast its lifetime (as there is no successor state to clean it up).
    /// </remarks>
    public abstract class GameStateBehaviour : LifetimeScope
    {
        /// <summary>
        /// What GameState this represents. Server and client specializations of a state should always return the same enum.
        /// </summary>
        public abstract GameState ActiveState { get; }

        /// <summary>
        /// This is the single active GameState object. There can be only one.
        /// </summary>
        private static GameObject s_ActiveStateGO;

        protected override void Awake()
        {
            base.Awake();

            if (Parent != null)
            {
                Parent.Container.Inject(this);
            }
            if (DedicatedServerUtilities.IsServerBuildTarget)
            {
                DedicatedServerUtilities.Log($"Entering game state:[{GetType().Name}], waiting for player joining and making their selection");
            }
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (s_ActiveStateGO != null)
            {
                if (s_ActiveStateGO == gameObject || s_ActiveStateGO.GetComponent<GameStateBehaviour>().ActiveState == ActiveState)
                {
                    //nothing to do here, if we're already the active state object.
                    return;
                }

                //the old state is going away. Either it wasn't a Persisting state, or it was,
                //but we're a different kind of state. In either case, we're going to be replacing it.
                Destroy(s_ActiveStateGO);
            }

            s_ActiveStateGO = gameObject;
        }

        protected override void OnDestroy()
        {
            s_ActiveStateGO = null;
        }
    }
}
