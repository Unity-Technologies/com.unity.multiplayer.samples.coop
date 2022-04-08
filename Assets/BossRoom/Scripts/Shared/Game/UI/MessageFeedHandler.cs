using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the display of in-game messages in a message feed
    /// </summary>
    public class MessageFeedHandler : NetworkBehaviour
    {
        public readonly struct MessageFeed
        {
            public readonly LinkedList<string> messages;

            public MessageFeed(LinkedList<string> messages)
            {
                this.messages = messages;
            }
        }

        MessageFeed m_Feed = new MessageFeed(new LinkedList<string>());

        static MessageFeedHandler s_Instance;

        IPublisher<MessageFeed> m_Publisher;

        [Inject]
        void InjectDependencies(IPublisher<MessageFeed> publisher)
        {
            m_Publisher = publisher;
        }

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
            DontDestroyOnLoad(this);
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
            m_Feed.messages.AddFirst(message);
            m_Publisher.Publish(m_Feed);
        }
    }
}
