using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class SceneLoaderWrapper : NetworkBehaviour
    {
        /// <summary>
        /// Manages a loading screen by wrapping around scene management APIs. It loads scene using the SceneManager,
        /// or, on listening servers for which scene management is enabled, using the NetworkSceneManager and handles
        /// the starting and stopping of the loading screen.
        /// </summary>

        [SerializeField]
        ClientLoadingScreen m_ClientLoadingScreen;

        [SerializeField]
        LoadingProgressManager m_LoadingProgressManager;

        bool IsNetworkSceneManagementEnabled => NetworkManager != null && NetworkManager.SceneManager != null && NetworkManager.NetworkConfig.EnableSceneManagement;

        bool m_IsInitialized;

        public static SceneLoaderWrapper Instance { get; protected set; }

        public virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            DontDestroyOnLoad(this);
        }

        public virtual void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            NetworkManager.OnServerStarted += OnNetworkingSessionStarted;
            NetworkManager.OnClientStarted += OnNetworkingSessionStarted;
            NetworkManager.OnServerStopped += OnNetworkingSessionEnded;
            NetworkManager.OnClientStopped += OnNetworkingSessionEnded;
        }

        void OnNetworkingSessionStarted()
        {
            // This prevents this to be called twice on a host, which receives both OnServerStarted and OnClientStarted callbacks
            if (!m_IsInitialized)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                }

                m_IsInitialized = true;
            }
        }

        void OnNetworkingSessionEnded(bool unused)
        {
            if (m_IsInitialized)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                }

                m_IsInitialized = false;
            }
        }

        public override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (NetworkManager != null)
            {
                NetworkManager.OnServerStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnClientStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnServerStopped -= OnNetworkingSessionEnded;
                NetworkManager.OnClientStopped -= OnNetworkingSessionEnded;
            }
            base.OnDestroy();
        }

        /// <summary>
        /// Loads a scene asynchronously using the specified loadSceneMode, with NetworkSceneManager if on a listening
        /// server with SceneManagement enabled, or SceneManager otherwise. If a scene is loaded via SceneManager, this
        /// method also triggers the start of the loading screen.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <param name="useNetworkSceneManager">If true, uses NetworkSceneManager, else uses SceneManager</param>
        /// <param name="loadSceneMode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        public virtual void LoadScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (useNetworkSceneManager)
            {
                if (IsSpawned && IsNetworkSceneManagementEnabled && !NetworkManager.ShutdownInProgress)
                {
                    if (NetworkManager.IsServer)
                    {
                        // If is active server and NetworkManager uses scene management, load scene using NetworkManager's SceneManager
                        NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                    }
                }
            }
            else
            {
                // Load using SceneManager
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (loadSceneMode == LoadSceneMode.Single)
                {
                    m_ClientLoadingScreen.StartLoadingScreen(sceneName);
                    m_LoadingProgressManager.LocalLoadOperation = loadOperation;
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!IsSpawned || NetworkManager.ShutdownInProgress)
            {
                m_ClientLoadingScreen.StopLoadingScreen();
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load: // Server told client to load a scene
                    // Only executes on client or host
                    if (NetworkManager.IsClient)
                    {
                        // Only start a new loading screen if scene loaded in Single mode, else simply update
                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                        {
                            m_ClientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName);
                            m_LoadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                        }
                        else
                        {
                            m_ClientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName);
                            m_LoadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                        }
                    }
                    break;
                case SceneEventType.LoadEventCompleted: // Server told client that all clients finished loading a scene
                    // Only executes on client or host
                    if (NetworkManager.IsClient)
                    {
                        m_ClientLoadingScreen.StopLoadingScreen();
                    }
                    break;
                case SceneEventType.Synchronize: // Server told client to start synchronizing scenes
                {
                    // Only executes on client that is not the host
                    if (NetworkManager.IsClient && !NetworkManager.IsHost)
                    {
                        if (NetworkManager.SceneManager.ClientSynchronizationMode == LoadSceneMode.Single)
                        {
                            // If using the Single ClientSynchronizationMode, unload all currently loaded additive
                            // scenes. In this case, we want the client to only keep the same scenes loaded as the
                            // server. Netcode For GameObjects will automatically handle loading all the scenes that the
                            // server has loaded to the client during the synchronization process. If the server's main
                            // scene is different to the client's, it will start by loading that scene in single mode,
                            // unloading every additively loaded scene in the process. However, if the server's main
                            // scene is the same as the client's, it will not automatically unload additive scenes, so
                            // we do it manually here.
                            UnloadAdditiveScenes();
                        }
                    }
                    break;
                }
                case SceneEventType.SynchronizeComplete: // Client told server that they finished synchronizing
                    // Only executes on server
                    if (NetworkManager.IsServer)
                    {
                        // Send client RPC to make sure the client stops the loading screen after the server handles what it needs to after the client finished synchronizing, for example character spawning done server side should still be hidden by loading screen.
                        ClientStopLoadingScreenRpc(RpcTarget.Group(new[] { sceneEvent.ClientId }, RpcTargetUse.Temp));
                    }
                    break;
            }
        }

        void UnloadAdditiveScenes()
        {
            var activeScene = SceneManager.GetActiveScene();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != activeScene)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void ClientStopLoadingScreenRpc(RpcParams clientRpcParams = default)
        {
            m_ClientLoadingScreen.StopLoadingScreen();
        }
    }
}
