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

        [SerializeField]
        List<Slider> m_OtherPlayersProgressBars;

        [SerializeField]
        protected LoadingProgressManager m_LoadingProgressManager;

        protected Dictionary<ulong, int> m_ClientIdToProgressBarsIndex = new Dictionary<ulong, int>();

        bool m_LoadingScreenRunning;

        Coroutine m_FadeOutCoroutine;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            m_CanvasGroup.alpha = 0;
            m_LoadingProgressManager.onTrackersUpdated += OnProgressTrackersUpdated;
        }

        void OnDestroy()
        {
            m_LoadingProgressManager.onTrackersUpdated -= OnProgressTrackersUpdated;
        }

        void Update()
        {
            if (m_LoadingScreenRunning)
            {
                m_ProgressBar.value = m_LoadingProgressManager.LocalProgress;
                UpdateOtherPlayersProgressBars();
            }
        }

        void OnProgressTrackersUpdated()
        {
            if (!m_LoadingScreenRunning)
            {
                // no need to handle it here, since it will be reinitialized when the next loading screen starts anyway
                return;
            }

            // deactivate progress bars of clients that are no longer tracked
            var clientIdsToRemove = new List<ulong>();
            foreach (var clientId in m_ClientIdToProgressBarsIndex.Keys)
            {
                if (!m_LoadingProgressManager.ProgressTrackers.ContainsKey(clientId))
                {
                    clientIdsToRemove.Add(clientId);
                }
            }

            foreach (var clientId in clientIdsToRemove)
            {
                RemoveOtherPlayerProgressBar(clientId);
            }

            foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
            {
                var clientId = progressTracker.Key;
                if (clientId != NetworkManager.Singleton.LocalClientId && !m_ClientIdToProgressBarsIndex.ContainsKey(clientId))
                {
                    AddOtherPlayerProgressBar(clientId);
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
            ReinitializeProgressBars();
        }

        protected virtual void ReinitializeProgressBars()
        {
            // clear map
            m_ClientIdToProgressBarsIndex.Clear();

            // deactivate all other players' progress bars
            foreach (var progressBar in m_OtherPlayersProgressBars)
            {
                progressBar.gameObject.SetActive(false);
            }

            foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
            {
                var clientId = progressTracker.Key;
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    AddOtherPlayerProgressBar(clientId);
                }
            }
        }

        protected virtual void AddOtherPlayerProgressBar(ulong clientId)
        {
            if (m_ClientIdToProgressBarsIndex.Count < m_OtherPlayersProgressBars.Count)
            {
                m_ClientIdToProgressBarsIndex[clientId] = m_ClientIdToProgressBarsIndex.Count;
                m_OtherPlayersProgressBars[m_ClientIdToProgressBarsIndex[clientId]].value = 0;
                m_OtherPlayersProgressBars[m_ClientIdToProgressBarsIndex[clientId]].gameObject.SetActive(true);
            }
            else
            {
                throw new Exception("There are not enough progress bars to track the progress of all the players.");
            }
        }

        protected virtual void RemoveOtherPlayerProgressBar(ulong clientId)
        {
            m_OtherPlayersProgressBars[m_ClientIdToProgressBarsIndex[clientId]].gameObject.SetActive(false);
            m_ClientIdToProgressBarsIndex.Remove(clientId);
        }

        protected virtual void UpdateOtherPlayersProgressBars()
        {
            foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
            {
                var clientId = progressTracker.Key;
                var progress = progressTracker.Value.Progress;
                if (clientId != NetworkManager.Singleton.LocalClientId && m_ClientIdToProgressBarsIndex.ContainsKey(clientId))
                {
                    m_OtherPlayersProgressBars[m_ClientIdToProgressBarsIndex[clientId]].value = progress.Value;
                }
            }
        }

        public void UpdateLoadingScreen(string sceneName)
        {
            if (m_LoadingScreenRunning)
            {
                m_SceneName.text = sceneName;
                if (m_FadeOutCoroutine != null)
                {
                    StopCoroutine(m_FadeOutCoroutine);
                }
            }
        }

        IEnumerator FadeOutCoroutine()
        {
            yield return new WaitForSeconds(m_DelayBeforeFadeOut);
            m_LoadingScreenRunning = false;

            float currentTime = 0;
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
