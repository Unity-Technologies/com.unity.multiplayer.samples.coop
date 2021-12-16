using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        List<string> m_ScenesTriggeringLoadingScreen;

        [SerializeField]
        List<string> m_ScenesEndingLoadingScreen;

        [SerializeField]
        ClientLoadingScreen m_ClientLoadingScreen;

        [SerializeField]
        NetworkManager m_NetworkManager;

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
            if (m_NetworkManager)
            {
                if (m_NetworkManager.SceneManager != null)
                {
                    m_NetworkManager.SceneManager.OnSceneEvent -= NotifyLoadingScreen;
                }
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // This needs to be called right after initializing NetworkManager (after StartHost, StartClient or StartServer)
        public void AddOnSceneEventCallback()
        {
            if (m_NetworkManager.SceneManager != null)
            {
                m_NetworkManager.SceneManager.OnSceneEvent += NotifyLoadingScreen;
            }
        }

        public void LoadScene(string sceneName, LoadSceneMode loadSceneMode)
        {
            if (m_NetworkManager != null && m_NetworkManager.IsListening)
            {
                if (m_NetworkManager.IsServer)
                {
                    // If is active server and NetworkManager uses scene management, load scene using NetworkManager's SceneManager
                    m_NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
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
            if (m_ScenesEndingLoadingScreen.Contains(scene.name))
            {
                m_ClientLoadingScreen.StopLoadingScreen();
            }
        }

        void NotifyLoadingScreen(SceneEvent sceneEvent)
        {
            // Only executes on client
            if (m_NetworkManager != null && m_NetworkManager.IsClient)
            {
                switch (sceneEvent.SceneEventType)
                {
                    case SceneEventType.Unload:
                    case SceneEventType.Load:
                        if (sceneEvent.ClientId == m_NetworkManager.LocalClientId)
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
                }
            }
        }
    }
}
