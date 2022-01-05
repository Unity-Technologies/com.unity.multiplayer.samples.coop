using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class SceneLoader : NetworkBehaviour
    {
        /// <summary>
        /// This singleton is a utility handling the loading of scenes. It loads scene using the SceneManager, or, on
        /// listening servers for which scene management is enabled, using the NetworkSceneManager. It also starts and
        /// stops the loading screen.
        /// </summary>

        [SerializeField]
        ClientLoadingScreen m_ClientLoadingScreen;

        public static SceneLoader Instance { get; private set; }

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

        public override void OnDestroy()
        {
            if (NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // This needs to be called right after initializing NetworkManager (after StartHost, StartClient or StartServer)
        public void AddOnSceneEventCallback()
        {
            if (NetworkManager != null && NetworkManager.SceneManager != null && NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        public void LoadScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            if (NetworkManager != null && NetworkManager.IsListening)
            {
                if (NetworkManager.IsServer)
                {
                    if (NetworkManager.NetworkConfig.EnableSceneManagement)
                    {
                        // If is active server and NetworkManager uses scene management, load scene using NetworkManager's SceneManager
                        NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                    }
                    else
                    {
                        LoadUsingSceneManager(sceneName, loadSceneMode);
                    }
                }
            }
            else
            {
                // If offline, load using SceneManager
                LoadUsingSceneManager(sceneName, loadSceneMode);
            }
        }

        void LoadUsingSceneManager(string sceneName, LoadSceneMode loadSceneMode)
        {
            var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (loadSceneMode == LoadSceneMode.Single)
            {
                m_ClientLoadingScreen.StartLoadingScreen(sceneName, loadOperation);
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                m_ClientLoadingScreen.StopLoadingScreen();
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load: // Server told client to load a scene
                    // Only executes on client
                    if (IsClient)
                    {
                        // Only start a new loading screen if scene loaded in Single mode, else simply update
                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                        {
                            m_ClientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName, sceneEvent.AsyncOperation);
                        }
                        else
                        {
                            m_ClientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName, sceneEvent.AsyncOperation);
                        }
                    }
                    break;
                case SceneEventType.LoadEventCompleted: // Server told client that all clients finished loading a scene
                    // Only executes on client
                    if (IsClient)
                    {
                        m_ClientLoadingScreen.StopLoadingScreen();
                    }
                    break;
                case SceneEventType.SynchronizeComplete: // Client told server that they finished synchronizing
                    // Only executes on server
                    if (IsServer)
                    {
                        // Send client RPC to make sure the client stops the loading screen after the server handles what it needs to after the client finished synchronizing
                        StopLoadingScreenClientRpc(new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = new[] {sceneEvent.ClientId}}});
                    }
                    break;
            }
        }

        [ClientRpc]
        void StopLoadingScreenClientRpc(ClientRpcParams clientRpcParams = default)
        {
            m_ClientLoadingScreen.StopLoadingScreen();
        }
    }
}
