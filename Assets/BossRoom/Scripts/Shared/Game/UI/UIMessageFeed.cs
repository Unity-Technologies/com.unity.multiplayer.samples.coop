using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the display of in-game messages in a message feed
    /// </summary>
    public class UIMessageFeed : MonoBehaviour
    {
        [SerializeField]
        List<TMPro.TextMeshProUGUI> m_TextLabels;

        [SerializeField]
        float m_HideDelay = 5;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        Coroutine m_HideFeedCoroutine;

        bool m_IsDisplaying;

        IDisposable m_Subscription;

        [Inject]
        void InjectDependencies(ISubscriber<MessageFeedHandler.MessageFeed> subscriber)
        {
            m_Subscription = subscriber.Subscribe(DisplayMessages);
        }

        void Start()
        {
            Hide();
        }

        void OnDestroy()
        {
            m_Subscription?.Dispose();
        }

        void Show()
        {
            if (!m_IsDisplaying)
            {
                m_IsDisplaying = true;
                m_CanvasGroup.alpha = 1;
            }
            if (m_HideFeedCoroutine != null)
            {
                StopCoroutine(m_HideFeedCoroutine);
            }

            m_HideFeedCoroutine = StartCoroutine(HideFeedCoroutine());
        }

        void Hide()
        {
            m_IsDisplaying = false;
            m_CanvasGroup.alpha = 0;
        }

        IEnumerator HideFeedCoroutine()
        {
            yield return new WaitForSeconds(m_HideDelay);
            Hide();
        }

        void DisplayMessages(MessageFeedHandler.MessageFeed messageFeed)
        {
            var message = messageFeed.messages.First;
            for (var i = 0; i < m_TextLabels.Count; i++)
            {
                if (message != null)
                {
                    m_TextLabels[i].text = message.Value;
                    message = message.Next;
                }
                else
                {
                    m_TextLabels[i].text = "";
                }
            }
            Show();
        }
    }
}
