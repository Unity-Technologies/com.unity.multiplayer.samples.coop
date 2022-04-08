using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the publishing of messages for an in-game feed. When the server creates a message through it, it sends
    /// it to all clients, who then publish the feed history for all subscribers.
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

        public override void OnNetworkDespawn()
        {
            m_Feed.messages.Clear();
        }

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Adds a new message to the in-game feed for all clients to publish. Must be called on the server.
        /// </summary>
        /// <param name="message">The message to publish</param>
        public static void PublishMessage(string message)
        {
            if (s_Instance != null)
            {
                if (s_Instance.IsServer)
                {
                    s_Instance.PublishInGameFeedMessageClientRpc(message);
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
        void PublishInGameFeedMessageClientRpc(string message)
        {
            m_Feed.messages.AddFirst(message);
            m_Publisher.Publish(m_Feed);
        }
    }
}
