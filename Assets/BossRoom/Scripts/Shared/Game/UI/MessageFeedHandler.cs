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
    public class MessageFeedHandler : NetworkBehaviour
    {
        UIMessageFeed m_MessageFeed;

        LinkedList<string> m_Messages = new LinkedList<string>();

        static MessageFeedHandler s_Instance;

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
        }

        public static void SetMessageFeed(UIMessageFeed messageFeed)
        {
            if (s_Instance != null)
            {
                s_Instance.m_MessageFeed = messageFeed;
            }
            else
            {
                Debug.LogError($"No MessageFeedHandler instance found.");
            }
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
                Debug.LogError($"No MessageFeedHandler instance found. Cannot display message: {message}");
            }
        }

        [ClientRpc]
        void ShowInGameFeedMessageClientRpc(string message)
        {
            m_Messages.AddFirst(message);
            m_MessageFeed.Show(m_Messages);
        }
    }
}
