using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// This script handles the use of a loading screen with a progress bar and the name of the loaded scene shown. It
    /// must be started and stopped from outside this script. It also allows updating the loading screen when a new
    /// loading operation starts before the loading screen is stopped.
    /// </summary>
    public class ClientLoadingScreen : NetworkBehaviour
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

        [SerializeField]
        List<Slider> m_OtherPlayersProgressBars;

        [SerializeField]
        LoadingProgressManager m_LoadingProgressManager;

        Dictionary<ulong, int> m_ClientIdToProgressBarsIndex = new Dictionary<ulong, int>();

        bool m_LoadingScreenRunning;

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
            if (m_LoadingScreenRunning)
            {
                m_ProgressBar.value = m_LoadingProgressManager.LocalProgress;

                if (IsSpawned)
                {
                    foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
                    {
                        var clientId = progressTracker.Key;
                        var progress = progressTracker.Value.Progress;
                        if (clientId == NetworkManager.LocalClientId)
                        {
                            m_ProgressBar.value = progress;
                        }
                        else
                        {
                            if (!m_ClientIdToProgressBarsIndex.ContainsKey(clientId))
                            {
                               m_ClientIdToProgressBarsIndex[clientId] = m_ClientIdToProgressBarsIndex.Count;
                            }
                            m_OtherPlayersProgressBars[m_ClientIdToProgressBarsIndex[clientId]].value = progress;
                        }
                    }
                }
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

        public void StartLoadingScreen(string sceneName)
        {
            m_CanvasGroup.alpha = 1;
            m_LoadingScreenRunning = true;
            UpdateLoadingScreen(sceneName);
        }

        public void UpdateLoadingScreen(string sceneName)
        {
            if (m_LoadingScreenRunning)
            {
                m_SceneName.text = sceneName;
                m_ProgressBar.value = 0;
            }
        }

        IEnumerator FadeOutCoroutine()
        {
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
