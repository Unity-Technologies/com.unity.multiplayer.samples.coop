using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class ClientLoadingScreen : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        float m_FadeDuration = 1;

        [SerializeField]
        NetworkManager m_NetworkManager;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            m_CanvasGroup.alpha = 0;
            if (m_NetworkManager)
            {
                m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
                m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnLoaded;
        }

        void OnDestroy()
        {
            if (m_NetworkManager)
            {
                m_NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                if (m_NetworkManager.SceneManager != null)
                {
                    m_NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                }
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnLoaded;
        }

        void OnSceneUnLoaded(Scene scene)
        {
            StartLoadingScreen();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            StartCoroutine(FadeCoroutine());
        }

        void OnClientConnected(ulong clientId)
        {
            if (m_NetworkManager.SceneManager != null)
            {
                m_NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnLoaded;
            }
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (m_NetworkManager.SceneManager != null)
            {
                m_NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadEventCompleted:
                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                    {
                        StartCoroutine(FadeCoroutine());
                    }
                    break;
                case SceneEventType.SynchronizeComplete:
                    StartCoroutine(FadeCoroutine());
                    break;
                case SceneEventType.Load:
                    if (sceneEvent.ClientId == m_NetworkManager.LocalClientId && sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                    {
                        StartLoadingScreen();
                    }
                    break;
                case SceneEventType.Synchronize:
                    if (sceneEvent.ClientId == m_NetworkManager.LocalClientId)
                    {
                        StartLoadingScreen();
                    }
                    break;
            }
        }

        public void StartLoadingScreen()
        {
            m_CanvasGroup.alpha = 1;
        }

        IEnumerator FadeCoroutine()
        {
            float startTime = Time.time;
            float currentTime = startTime;
            while (currentTime < startTime + m_FadeDuration)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(1, 0, (currentTime - startTime) / m_FadeDuration);
                yield return null;
                currentTime += Time.deltaTime;
            }

            m_CanvasGroup.alpha = 0;
        }
    }
}
