using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    /// <summary>
    /// This type of message channel allows the server to publish a message that will be sent to clients as well as
    /// being published locally. Clients and the server both can subscribe to it. However, that subscription needs to be
    /// done after the NetworkManager has initialized. On objects whose lifetime is bigger than a networked session,
    /// subscribing will be required each time a new session starts.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkedMessageChannel<T> : MessageChannel<T> where T : unmanaged, INetworkSerializeByMemcpy
    {
        NetworkManager m_NetworkManager;

        string m_Name;

        public NetworkedMessageChannel(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_Name = $"{typeof(T).FullName}NetworkMessageChannel";
        }

        public override void Dispose()
        {
            if (!IsDisposed)
            {
                if (m_NetworkManager != null && m_NetworkManager.CustomMessagingManager != null)
                {
                    m_NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(m_Name);
                }
            }
            base.Dispose();
        }

        public override IDisposable Subscribe(Action<T> handler)
        {
            if (m_NetworkManager != null)
            {
                if (m_NetworkManager.IsListening)
                {
                    RegisterHandler();
                }
                else
                {
                    m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
                }

                return base.Subscribe(handler);
            }

            Debug.LogError("Cannot subscribe to NetworkedMessageChannel. NetworkManager is null.");
            return null;
        }

        void RegisterHandler()
        {
            // Only register message handler on clients
            if (!m_NetworkManager.IsServer)
            {
                Debug.Log($"Registering handler for {m_Name}");
                m_NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(m_Name, ReceiveMessageThroughNetwork);
            }
        }

        void OnClientConnected(ulong clientId)
        {
            RegisterHandler();
        }

        public override void Publish(T message)
        {
            if (m_NetworkManager.IsServer)
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
            var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize<T>(), Allocator.Temp);
            writer.WriteValueSafe(message);
            m_NetworkManager.CustomMessagingManager.SendNamedMessageToAll(m_Name, writer);
        }

        void ReceiveMessageThroughNetwork(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out T message);
            base.Publish(message);
        }
    }
}
