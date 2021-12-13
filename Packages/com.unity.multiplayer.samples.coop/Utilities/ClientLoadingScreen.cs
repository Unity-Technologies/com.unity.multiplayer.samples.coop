using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class ClientLoadingScreen : NetworkBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        float m_FadeDuration = 1;

        void Start()
        {
            m_CanvasGroup.alpha = 0;
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                    {
                        StartCoroutine(FadeCoroutine());
                    }
                    break;
                case SceneEventType.SynchronizeComplete:
                    StartCoroutine(FadeCoroutine());
                    break;
                case SceneEventType.Load:
                    if (sceneEvent.ClientId == NetworkManager.LocalClientId && sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                    {
                        Time.timeScale = 0;
                        m_CanvasGroup.alpha = 1;
                    }
                    break;
                case SceneEventType.Synchronize:
                    if (sceneEvent.ClientId == NetworkManager.LocalClientId)
                    {
                        Time.timeScale = 0;
                        m_CanvasGroup.alpha = 1;
                    }
                    break;
            }
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
            Time.timeScale = 1;
        }
    }
}
