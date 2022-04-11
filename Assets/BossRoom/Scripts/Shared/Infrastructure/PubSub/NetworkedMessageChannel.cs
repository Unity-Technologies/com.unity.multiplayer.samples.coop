using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    public class NetworkedMessageChannel<T> : MessageChannel<T> where T : unmanaged
    {
        string m_Name;

        int m_BufferSize;

        bool m_HasRegisteredHandler;

        public NetworkedMessageChannel(string name, int bufferSize)
        {
            m_Name = name;
            m_BufferSize = bufferSize;
        }

        ~NetworkedMessageChannel()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(m_Name);
            }
        }

        public override IDisposable Subscribe(Action<T> handler)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                if (!m_HasRegisteredHandler)
                {
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(m_Name, ReceiveMessageThroughNetwork);
                    m_HasRegisteredHandler = true;
                }

                return base.Subscribe(handler);
            }

            return null;
        }

        public override void Publish(T message)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                // send message to clients, then publish locally
                SendMessageThroughNetwork(message);
                base.Publish(message);
            }
            else
            {
                Debug.LogError("Only a server can publish in a NetworkedMessageChannel");
            }
        }

        void SendMessageThroughNetwork(T message)
        {
            var writer = new FastBufferWriter(m_BufferSize, Allocator.Temp);
            writer.WriteValueSafe(message);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(m_Name, writer);
        }

        void ReceiveMessageThroughNetwork(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out T message);
            base.Publish(message);
        }
    }
}
