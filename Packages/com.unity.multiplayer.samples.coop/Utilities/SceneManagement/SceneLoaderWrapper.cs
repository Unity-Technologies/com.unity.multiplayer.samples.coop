using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class SceneLoaderWrapper : MonoBehaviour
    {
        /// <summary>
        /// Manages a loading screen by wrapping around scene management APIs. It loads scene using the SceneManager,
        /// or, on listening servers for which scene management is enabled, using the NetworkSceneManager and handles
        /// the starting and stopping of the loading screen.
        /// </summary>

        [SerializeField]
        ClientLoadingScreen m_ClientLoadingScreen;

        [SerializeField]
        NetworkManager m_NetworkManager;

        public static SceneLoaderWrapper Instance { get; private set; }

        public bool IsClosingClients { get; set; }

        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnDestroy()
        {
            if (m_NetworkManager != null && m_NetworkManager.SceneManager != null)
            {
                m_NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Initializes the callback on scene events. This needs to be called right after initializing NetworkManager
        /// (after StartHost, StartClient or StartServer)
        /// </summary>
        public void AddOnSceneEventCallback()
        {
            if (m_NetworkManager != null && m_NetworkManager.SceneManager != null && m_NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                m_NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        /// <summary>
        /// Loads a scene asynchronously using the specified loadSceneMode, with NetworkSceneManager if on a listening
        /// server with SceneManagement enabled, or SceneManager otherwise. If a scene is loaded via SceneManager, this
        /// method also triggers the start of the loading screen.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <param name="loadSceneMode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        public void LoadScene(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (IsOffline() || IsClosingClients)
            {
                // If offline, load using SceneManager
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (loadSceneMode == LoadSceneMode.Single)
                {
                    m_ClientLoadingScreen.StartLoadingScreen(sceneName, loadOperation);
                }
            }
            else
            {
                if (m_NetworkManager.IsServer)
                {
                    // If is active server and NetworkManager uses scene management, load scene using NetworkManager's SceneManager
                    m_NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // we're letting networked scene loading handle our loading screen, since there's still some loading happening after unity scene loading is done.
            // in case we're offline, then we let unity's scene loader tell us when to turn off the loading screen.
            // TODO this can be called while we're still in the WaitForSeconds(0.5) while shutting down host side. which means we'll still be in theory connected. Waiting on fix
            // for this in MTT-2821
            if (IsOffline() || IsClosingClients)
            {
                m_ClientLoadingScreen.StopLoadingScreen();
            }
        }

        bool IsOffline()
        {
            return m_NetworkManager == null || !m_NetworkManager.IsListening || m_NetworkManager.ShutdownInProgress || !m_NetworkManager.NetworkConfig.EnableSceneManagement;
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            // Only executes on client
            if (m_NetworkManager.IsClient)
            {
                switch (sceneEvent.SceneEventType)
                {
                    case SceneEventType.Load: // Server told client to load a scene
                        // Only start a new loading screen if scene loaded in Single mode, else simply update
                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                        {
                            m_ClientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName, sceneEvent.AsyncOperation);
                        }
                        else
                        {
                            m_ClientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName, sceneEvent.AsyncOperation);
                        }
                        break;
                    case SceneEventType.LoadEventCompleted: // Server told client that all clients finished loading a scene
                        m_ClientLoadingScreen.StopLoadingScreen();
                        break;
                    case SceneEventType.SynchronizeComplete: // Client has finished synchronizing
                        m_ClientLoadingScreen.StopLoadingScreen(true);
                        break;
                }
            }
        }
    }
}
