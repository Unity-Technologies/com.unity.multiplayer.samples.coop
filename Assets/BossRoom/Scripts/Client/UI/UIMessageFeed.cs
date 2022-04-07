using System;
using System.Collections;
using System.Collections.Generic;
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

        LinkedList<string> m_Messages = new LinkedList<string>();

        void OnMessageReceived(string message)
        {
            if (m_Messages.Count == m_TextLabels.Count)
            {
                m_Messages.RemoveLast();
            }
            m_Messages.AddFirst(message);
            DisplayMessages();
        }

        void DisplayMessages()
        {
            var i = 0;
            foreach (var message in m_Messages)
            {
                m_TextLabels[i++].text = message;
            }
        }
    }
}
