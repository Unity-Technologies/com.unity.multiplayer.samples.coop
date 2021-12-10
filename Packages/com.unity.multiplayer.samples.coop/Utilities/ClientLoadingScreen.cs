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
        CanvasGroup canvasGroup;

        [SerializeField]
        float fadeDuration = 2;

        void Start()
        {
            canvasGroup.alpha = 0;
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            base.OnNetworkSpawn();
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
                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                    {
                        canvasGroup.alpha = 1;
                    }
                    break;
                case SceneEventType.Synchronize:
                    canvasGroup.alpha = 1;
                    break;
            }
        }

        IEnumerator FadeCoroutine()
        {
            float startTime = Time.time;
            float currentTime = startTime;
            while (currentTime < startTime + fadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0, 1, (currentTime - startTime) / fadeDuration);
                yield return null;
                currentTime += Time.deltaTime;
            }

            canvasGroup.alpha = 1;
        }
    }
}
