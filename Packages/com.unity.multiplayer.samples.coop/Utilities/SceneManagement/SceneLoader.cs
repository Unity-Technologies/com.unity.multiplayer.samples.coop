using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class SceneLoader : NetworkBehaviour
    {
        [SerializeField]
        List<string> m_ScenesTriggeringLoadingScreen;

        [SerializeField]
        List<string> m_ScenesEndingLoadingScreen;

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

        void OnDestroy()
        {
            if (NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // This needs to be called right after initializing NetworkManager (after StartHost, StartClient or StartServer)
        public void AddOnSceneEventCallback()
        {
            if (NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        public void LoadScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            if (NetworkManager.IsListening)
            {
                if (NetworkManager.IsServer)
                {
                    // If is active server and NetworkManager uses scene management, load scene using NetworkManager's SceneManager
                    NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                }
            }
            else
            {
                // If offline, load using SceneManager
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (m_ScenesTriggeringLoadingScreen.Contains(sceneName))
                {
                    m_ClientLoadingScreen.StartLoadingScreen(sceneName, loadOperation);
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!NetworkManager.IsListening)
            {
                if (m_ScenesEndingLoadingScreen.Contains(scene.name))
                {
                    m_ClientLoadingScreen.StopLoadingScreen();
                }
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Unload:
                case SceneEventType.Load:
                case SceneEventType.Synchronize:
                    // Only executes on client
                    if (IsClient)
                    {
                        if (m_ScenesTriggeringLoadingScreen.Contains(sceneEvent.SceneName))
                        {
                            m_ClientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName, sceneEvent.AsyncOperation);
                        }
                        else
                        {
                            m_ClientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName, sceneEvent.AsyncOperation);
                        }
                    }
                    break;
                case SceneEventType.LoadEventCompleted:
                    // Only executes on client
                    if (IsClient)
                    {
                        if (m_ScenesEndingLoadingScreen.Contains(sceneEvent.SceneName))
                        {
                            m_ClientLoadingScreen.StopLoadingScreen();
                        }
                    }
                    break;
                case SceneEventType.SynchronizeComplete:
                    // Only executes on server
                    if (IsServer)
                    {
                        // Always stop loading screen after synchronizeComplete event
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
