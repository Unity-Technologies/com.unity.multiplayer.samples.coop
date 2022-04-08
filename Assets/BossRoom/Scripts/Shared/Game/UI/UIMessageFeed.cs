using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the display of in-game messages in a message feed
    /// </summary>
    public class UIMessageFeed : NetworkBehaviour
    {
        [SerializeField]
        List<TMPro.TextMeshProUGUI> m_TextLabels;

        [SerializeField]
        float m_HideDelay = 3;

        Coroutine m_HideFeedCoroutine;

        bool m_IsDisplaying;

        void Start()
        {
            MessageFeedHandler.SetMessageFeed(this);
            Hide();
        }

        public void Show(LinkedList<string> messages)
        {
            if (!m_IsDisplaying)
            {
                DisplayMessages(messages);
                m_IsDisplaying = true;
            }
            if (m_HideFeedCoroutine != null)
            {
                StopCoroutine(m_HideFeedCoroutine);
            }

            m_HideFeedCoroutine = StartCoroutine(HideFeedCoroutine());
        }

        IEnumerator HideFeedCoroutine()
        {
            yield return new WaitForSeconds(m_HideDelay);
            Hide();
        }

        void DisplayMessages(LinkedList<string> messages)
        {
            var message = messages.First;
            for (var i = 0; i < m_TextLabels.Count && message != null; i++)
            {
                m_TextLabels[i].text = message.Value;
                message = message.Next;
            }
        }

        void Hide()
        {
            foreach (var label in m_TextLabels)
            {
                label.text = "";
            }

            m_IsDisplaying = false;
        }
    }
}
