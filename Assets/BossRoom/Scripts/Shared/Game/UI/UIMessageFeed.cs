using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
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

        LinkedList<string> m_Messages = new LinkedList<string>();

        Coroutine m_HideFeedCoroutine;

        bool m_IsDisplaying;

        static UIMessageFeed s_Instance;

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
            Hide();
        }

        public void Show()
        {
            if (!m_IsDisplaying)
            {
                DisplayMessages();
                m_IsDisplaying = true;
            }
            if (m_HideFeedCoroutine != null)
            {
                StopCoroutine(m_HideFeedCoroutine);
            }

            m_HideFeedCoroutine = StartCoroutine(HideFeedCoroutine());
        }

        public static void ShowMessage(string message)
        {
            if (s_Instance != null)
            {
                if (s_Instance.IsServer)
                {
                    s_Instance.ShowInGameFeedMessageClientRpc(message);
                }
                else
                {
                    Debug.LogError("Only the server can display messages in the in-game feed.");
                }
            }
            else
            {
                Debug.LogError($"No UIMessageFeed instance found. Cannot display message: {message}");
            }
        }

        IEnumerator HideFeedCoroutine()
        {
            yield return new WaitForSeconds(m_HideDelay);
            Hide();
        }

        [ClientRpc]
        void ShowInGameFeedMessageClientRpc(string message)
        {
            if (m_Messages.Count == m_TextLabels.Count)
            {
                m_Messages.RemoveLast();
            }
            m_Messages.AddFirst(message);
            Show();
        }

        void DisplayMessages()
        {
            var i = 0;
            foreach (var message in m_Messages)
            {
                m_TextLabels[i++].text = message;
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
