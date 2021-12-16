using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class ClientLoadingScreen : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        float m_FadeDuration = 1;

        [SerializeField]
        Slider m_ProgressBar;

        [SerializeField]
        Text m_SceneName;

        bool m_LoadingScreenRunning;

        AsyncOperation m_LoadOperation;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            m_CanvasGroup.alpha = 0;
        }

        void Update()
        {
            if (m_LoadingScreenRunning && m_LoadOperation != null)
            {
                if (m_LoadOperation.isDone)
                {
                    m_ProgressBar.value = 1;
                }
                else
                {
                    m_ProgressBar.value = m_LoadOperation.progress;
                }
            }
        }

        public void StopLoadingScreen()
        {
            if (m_LoadingScreenRunning)
            {
                StartCoroutine(FadeCoroutine());
                m_LoadingScreenRunning = false;
            }
        }

        public void StartLoadingScreen(string sceneName, AsyncOperation loadOperation)
        {
            m_CanvasGroup.alpha = 1;
            m_LoadingScreenRunning = true;
            UpdateLoadingScreen(sceneName, loadOperation);
        }

        public void UpdateLoadingScreen(string sceneName, AsyncOperation loadOperation)
        {
            if (m_LoadingScreenRunning)
            {
                m_SceneName.text = sceneName;
                m_LoadOperation = loadOperation;
                m_ProgressBar.value = 0;
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
        }
    }
}
