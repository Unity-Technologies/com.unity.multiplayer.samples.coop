// ----------------------------------------------------------------------------------------------------------------------
// <summary>The Photon Chat Api enables clients to connect to a chat server and communicate with other clients.</summary>
// <remarks>ChatClient is the main class of this api.</remarks>
// <copyright company="Exit Games GmbH">Photon Chat Api - Copyright (C) 2014 Exit Games GmbH</copyright>
// ----------------------------------------------------------------------------------------------------------------------

#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif

namespace Photon.Chat
{
    using System.Collections.Generic;
    using System.Text;

    #if SUPPORTED_UNITY || NETFX_CORE
    using Hashtable = ExitGames.Client.Photon.Hashtable;
    using SupportClass = ExitGames.Client.Photon.SupportClass;
    #endif


    /// <summary>
    /// A channel of communication in Photon Chat, updated by ChatClient and provided as READ ONLY.
    /// </summary>
    /// <remarks>
    /// Contains messages and senders to use (read!) and display by your GUI.
    /// Access these by:
    ///     ChatClient.PublicChannels
    ///     ChatClient.PrivateChannels
    /// </remarks>
    public class ChatChannel
    {
        /// <summary>Name of the channel (used to subscribe and unsubscribe).</summary>
        public readonly string Name;

        /// <summary>Senders of messages in chronological order. Senders and Messages refer to each other by index. Senders[x] is the sender of Messages[x].</summary>
        public readonly List<string> Senders = new List<string>();

        /// <summary>Messages in chronological order. Senders and Messages refer to each other by index. Senders[x] is the sender of Messages[x].</summary>
        public readonly List<object> Messages = new List<object>();

        /// <summary>If greater than 0, this channel will limit the number of messages, that it caches locally.</summary>
        public int MessageLimit;

        /// <summary>Unique channel ID.</summary>
        public int ChannelID;

        /// <summary>Is this a private 1:1 channel?</summary>
        public bool IsPrivate { get; protected internal set; }

        /// <summary>Count of messages this client still buffers/knows for this channel.</summary>
        public int MessageCount { get { return this.Messages.Count; } }

        /// <summary>
        /// ID of the last message received.
        /// </summary>
        public int LastMsgId { get; protected set; }

        private Dictionary<object, object> properties;

        /// <summary>Whether or not this channel keeps track of the list of its subscribers.</summary>
        public bool PublishSubscribers { get; protected set; }

        /// <summary>Maximum number of channel subscribers. 0 means infinite.</summary>
        public int MaxSubscribers { get; protected set; }

        /// <summary>Subscribed users.</summary>
        public readonly HashSet<string> Subscribers = new HashSet<string>();

        /// <summary>Used internally to create new channels. This does NOT create a channel on the server! Use ChatClient.Subscribe.</summary>
        public ChatChannel(string name)
        {
            this.Name = name;
        }

        public bool TryGetProperty(string key, out object value)
        {
            if (properties == null)
            {
                value = default(object);
                return false;
            }

            return properties.TryGetValue(key, out value);
        }

        /// <summary>Used internally to add messages to this channel.</summary>
        public void Add(string sender, object message, int msgId)
        {
            this.Senders.Add(sender);
            this.Messages.Add(message);
            this.LastMsgId = msgId;
            this.TruncateMessages();
        }

        /// <summary>Used internally to add messages to this channel.</summary>
        public void Add(string[] senders, object[] messages, int lastMsgId)
        {
            this.Senders.AddRange(senders);
            this.Messages.AddRange(messages);
            this.LastMsgId = lastMsgId;
            this.TruncateMessages();
        }

        /// <summary>Reduces the number of locally cached messages in this channel to the MessageLimit (if set).</summary>
        public void TruncateMessages()
        {
            if (this.MessageLimit <= 0 || this.Messages.Count <= this.MessageLimit)
            {
                return;
            }

            int excessCount = this.Messages.Count - this.MessageLimit;
            this.Senders.RemoveRange(0, excessCount);
            this.Messages.RemoveRange(0, excessCount);
        }

        /// <summary>Clear the local cache of messages currently stored. This frees memory but doesn't affect the server.</summary>
        public void ClearMessages()
        {
            this.Senders.Clear();
            this.Messages.Clear();
        }

        /// <summary>Provides a string-representation of all messages in this channel.</summary>
        /// <returns>All known messages in format "Sender: Message", line by line.</returns>
        public string ToStringMessages()
        {
            StringBuilder txt = new StringBuilder();
            for (int i = 0; i < this.Messages.Count; i++)
            {
                txt.AppendLine(string.Format("{0}: {1}", this.Senders[i], this.Messages[i]));
            }
            return txt.ToString();
        }

        internal void ReadProperties(Dictionary<object, object> newProperties)
        {
            if (newProperties != null && newProperties.Count > 0)
            {
                if (this.properties == null)
                {
                    this.properties = new Dictionary<object, object>(newProperties.Count);
                }
                foreach (var k in newProperties.Keys)
                {
                    if (newProperties[k] == null)
                    {
                        if (this.properties.ContainsKey(k))
                        {
                            this.properties.Remove(k);
                        }
                    }
                    else
                    {
                        this.properties[k] = newProperties[k];
                    }
                }
                object temp;
                if (this.properties.TryGetValue(ChannelWellKnownProperties.PublishSubscribers, out temp))
                {
                    this.PublishSubscribers = (bool)temp;
                }
                if (this.properties.TryGetValue(ChannelWellKnownProperties.MaxSubscribers, out temp))
                {
                    this.MaxSubscribers = (int)temp;
                }
            }
        }

        internal void AddSubscribers(string[] users)
        {
            if (users == null)
            {
                return;
            }
            for (int i = 0; i < users.Length; i++)
            {
                this.Subscribers.Add(users[i]);
            }
        }

        internal void ClearProperties()
        {
            if (this.properties != null && this.properties.Count > 0)
            {
                this.properties.Clear();
            }
        }
    }
}