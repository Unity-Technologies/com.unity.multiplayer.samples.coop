using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// This script handles the use of a loading screen with a progress bar and the name of the loaded scene shown. It
    /// must be started and stopped from outside this script. It also allows updating the loading screen when a new
    /// loading operation starts before the loading screen is stopped.
    /// </summary>
    public class ClientLoadingScreen : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        float m_DelayBeforeFadeOut = 0.5f;

        [SerializeField]
        float m_FadeOutDuration = 0.1f;

        [SerializeField]
        Slider m_ProgressBar;

        [SerializeField]
        Text m_SceneName;

        bool m_LoadingScreenRunning;

        AsyncOperation m_LoadOperation;

        Coroutine m_FadeOutCoroutine;

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
                m_ProgressBar.value = m_LoadOperation.progress;
            }
        }

        public void StopLoadingScreen()
        {
            if (m_LoadingScreenRunning)
            {
                if (m_FadeOutCoroutine != null)
                {
                    StopCoroutine(m_FadeOutCoroutine);
                }
                m_FadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
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

        IEnumerator FadeOutCoroutine()
        {
            m_ProgressBar.value = 1;
            float currentTime = 0;
            while (currentTime < m_DelayBeforeFadeOut)
            {
                yield return null;
                currentTime += Time.deltaTime;
            }

            m_LoadingScreenRunning = false;
            currentTime = 0;
            while (currentTime < m_FadeOutDuration)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(1, 0, currentTime/ m_FadeOutDuration);
                yield return null;
                currentTime += Time.deltaTime;
            }

            m_CanvasGroup.alpha = 0;
        }
    }
}
